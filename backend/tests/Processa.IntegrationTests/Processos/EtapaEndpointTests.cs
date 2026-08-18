using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Processa.IntegrationTests.Identidade;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.IntegrationTests.Processos;

[Collection("ProcessaApi")]
public class EtapaEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Etapas",
            cnpj = CnpjTestHelper.GerarValido(),
            nomeAdmin = "Admin Teste",
            emailAdmin = email,
            senha = "senhaForte123",
        });

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "senhaForte123" });
        var accessToken = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return client;
    }

    private static async Task<Guid> CriarFluxoAsync(HttpClient client)
    {
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", new
        {
            equipeId,
            nome = "Abertura de empresa",
            descricao = (string?)null,
            responsavelObrigatorio = true,
            modoAtribuicao = "Manual",
            responsavelFixoId = (Guid?)null,
        });
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarFluxo = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo padrão", descricao = (string?)null, fluxoPadrao = false });
        return (await criarFluxo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task PostEtapas_TipoComumSemConfiguracao_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Revisão manual", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostEtapas_TipoAutomatizadaSemConfiguracao_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Chamada externa", descricao = (string?)null, tipo = "Automatizada", ordem = 0, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostEtapas_TipoAutomatizadaComUrlInvalida_Retorna422ComErrosPorCampo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas", new
        {
            nome = "Chamada externa",
            descricao = (string?)null,
            tipo = "Automatizada",
            ordem = 0,
            configuracao = new { tipo = "Automatizada", url = "não-é-uma-url", metodo = "Get", corpoTemplate = (string?)null, statusHttpEsperado = 200 },
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Configuracao").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostEtapas_TipoAutomatizadaComConfiguracaoValida_Retorna201EDetalheReflita()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);

        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas", new
        {
            nome = "Chamada externa",
            descricao = (string?)null,
            tipo = "Automatizada",
            ordem = 0,
            configuracao = new { tipo = "Automatizada", url = "https://exemplo.com/webhook", metodo = "Post", corpoTemplate = (string?)null, statusHttpEsperado = 200 },
        });
        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detalhe = await client.GetAsync($"/api/v1/etapas/{etapaId}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("configuracao").GetProperty("url").GetString().Should().Be("https://exemplo.com/webhook");
        body.GetProperty("configuracao").GetProperty("statusHttpEsperado").GetInt32().Should().Be(200);
    }

    [Fact]
    public async Task PostEtapas_FluxoInexistente_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{Guid.NewGuid()}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetEtapas_ListaEtapasDoFluxoOrdenadas()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Segunda", descricao = (string?)null, tipo = "Comum", ordem = 1, configuracao = (object?)null });
        await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Primeira", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });

        var resposta = await client.GetAsync($"/api/v1/fluxos/{fluxoId}/etapas");

        var etapas = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        etapas.Should().HaveCount(2);
        etapas[0].GetProperty("nome").GetString().Should().Be("Primeira");
    }

    [Fact]
    public async Task PutEtapa_DadosValidos_Retorna204()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Nome antigo", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PutAsJsonAsync($"/api/v1/etapas/{etapaId}",
            new { nome = "Nome novo", descricao = "Descrição", ordem = 1, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/etapas/{etapaId}");
        (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nome").GetString().Should().Be("Nome novo");
    }

    [Fact]
    public async Task DeleteEtapa_EtapaExiste_Retorna204()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.DeleteAsync($"/api/v1/etapas/{etapaId}");

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/v1/etapas/{etapaId}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutConfiguracaoAcesso_UsuarioValido_Retorna204EApareceNoDetalhe()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();

        var resposta = await client.PutAsJsonAsync($"/api/v1/etapas/{etapaId}/configuracao-acesso",
            new { podeAlterar = new[] { "Gestor" }, usuarioIdsPodeAlterar = new[] { usuarioId } });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/etapas/{etapaId}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("configuracaoAcesso").GetProperty("usuarioIdsPodeAlterar").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PutConfiguracaoAcesso_UsuarioInexistente_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PutAsJsonAsync($"/api/v1/etapas/{etapaId}/configuracao-acesso",
            new { podeAlterar = Array.Empty<string>(), usuarioIdsPodeAlterar = new[] { Guid.NewGuid() } });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostEtapas_TipoCondicionalComRamos_Retorna201EDetalhePreservaRamos()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var fluxoId = await CriarFluxoAsync(client);
        var campoId = Guid.NewGuid();
        var etapaDestinoId = Guid.NewGuid();
        var etapaPadraoId = Guid.NewGuid();

        var criar = await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas", new
        {
            nome = "Desvio por prioridade",
            descricao = (string?)null,
            tipo = "Condicional",
            ordem = 0,
            configuracao = new
            {
                tipo = "Condicional",
                ramos = new[] { new { campoPersonalizadoId = campoId, operador = "Igual", valor = "Alta", etapaDestinoId } },
                etapaPadraoId,
            },
        });

        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        var etapaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var detalhe = await client.GetAsync($"/api/v1/etapas/{etapaId}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("configuracao").GetProperty("etapaPadraoId").GetGuid().Should().Be(etapaPadraoId);
        body.GetProperty("configuracao").GetProperty("ramos").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PostEtapas_UsuarioAnalista_Retorna403()
    {
        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Teste", "analista-etapas@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{Guid.NewGuid()}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostEtapas_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync($"/api/v1/fluxos/{Guid.NewGuid()}/etapas",
            new { nome = "Nome", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
