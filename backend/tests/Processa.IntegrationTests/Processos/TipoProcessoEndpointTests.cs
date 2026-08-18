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
public class TipoProcessoEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Processos",
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

    private static async Task<(Guid EquipeId, Guid UsuarioId)> CriarEquipeComMembroAsync(HttpClient client, string papel = "Gestor")
    {
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();
        await client.PostAsJsonAsync($"/api/v1/equipes/{equipeId}/membros", new { usuarioId, papel });

        return (equipeId, usuarioId);
    }

    private static object NovoTipoProcessoPayload(Guid equipeId, Guid? responsavelFixoId = null) => new
    {
        equipeId,
        nome = "Abertura de empresa",
        descricao = "Processo de abertura",
        responsavelObrigatorio = true,
        modoAtribuicao = responsavelFixoId is null ? "Dinamico" : "Fixo",
        responsavelFixoId,
    };

    [Fact]
    public async Task PostTiposProcesso_DadosValidos_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);

        var resposta = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostTiposProcesso_EquipeNaoExiste_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(Guid.NewGuid()));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostTiposProcesso_ModoFixoResponsavelNaoEhMembroDaEquipe_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);

        var resposta = await client.PostAsJsonAsync(
            "/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId, Guid.NewGuid()));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostTiposProcesso_ModoFixoResponsavelEhMembroDaEquipe_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, usuarioId) = await CriarEquipeComMembroAsync(client);

        var resposta = await client.PostAsJsonAsync(
            "/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId, usuarioId));

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task GetTiposProcesso_ListaTiposDoTenant()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));

        var resposta = await client.GetAsync("/api/v1/tipos-processo");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task AtivarDesativarTipoProcesso_FluxoCompleto()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criar = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await client.PostAsync($"/api/v1/tipos-processo/{id}/desativar", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósDesativar = await client.GetAsync($"/api/v1/tipos-processo/{id}");
        (await apósDesativar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ativo").GetBoolean().Should().BeFalse();

        (await client.PostAsync($"/api/v1/tipos-processo/{id}/ativar", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósAtivar = await client.GetAsync($"/api/v1/tipos-processo/{id}");
        (await apósAtivar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ativo").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DefinirPermissoesInicio_UsuarioValido_Retorna204EApareceNoDetalhe()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, usuarioId) = await CriarEquipeComMembroAsync(client);
        var criar = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PutAsJsonAsync(
            $"/api/v1/tipos-processo/{id}/permissoes-inicio", new { perfis = new[] { "Gestor" }, usuarioIds = new[] { usuarioId } });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/tipos-processo/{id}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("permissoesInicio").GetProperty("usuarioIds").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task DefinirPermissoesInicio_UsuarioInexistente_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criar = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PutAsJsonAsync(
            $"/api/v1/tipos-processo/{id}/permissoes-inicio", new { perfis = Array.Empty<string>(), usuarioIds = new[] { Guid.NewGuid() } });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CampoPersonalizado_TipoListaSemOpcoes_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criar = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{id}/campos",
            new { nome = "Prioridade", tipo = "Lista", opcoes = (string[]?)null, obrigatorio = false, ordem = 0 });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Opcoes").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CampoPersonalizado_TipoNaoListaComOpcoes_Retorna422ComErrosPorCampo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criar = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{id}/campos",
            new { nome = "Observações", tipo = "Texto", opcoes = new[] { "não devia ter" }, obrigatorio = false, ordem = 0 });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Opcoes").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CampoPersonalizado_CriarAtualizarRemover_FluxoCompleto()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarCampo = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/campos",
            new { nome = "Prioridade", tipo = "Lista", opcoes = new[] { "Baixa", "Alta" }, obrigatorio = true, ordem = 0 });
        criarCampo.StatusCode.Should().Be(HttpStatusCode.Created);
        var campoId = (await criarCampo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detalheAposCriar = await client.GetAsync($"/api/v1/tipos-processo/{tipoId}");
        (await detalheAposCriar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campos").GetArrayLength().Should().Be(1);

        var atualizar = await client.PutAsJsonAsync($"/api/v1/campos-personalizados/{campoId}",
            new { nome = "Prioridade do processo", opcoes = new[] { "Baixa", "Média", "Alta" }, obrigatorio = false, ordem = 1 });
        atualizar.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remover = await client.DeleteAsync($"/api/v1/campos-personalizados/{campoId}");
        remover.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detalheAposRemover = await client.GetAsync($"/api/v1/tipos-processo/{tipoId}");
        (await detalheAposRemover.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("campos").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Fluxos_PrimeiroFluxoViraPadraoAutomaticamente()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarFluxo = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo padrão", descricao = (string?)null, fluxoPadrao = false });
        criarFluxo.StatusCode.Should().Be(HttpStatusCode.Created);

        var detalhe = await client.GetAsync($"/api/v1/tipos-processo/{tipoId}");
        var fluxos = (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fluxos");
        fluxos.GetArrayLength().Should().Be(1);
        fluxos[0].GetProperty("fluxoPadrao").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Fluxos_ExatamenteUmPadrao_TrocarPadraoDesmarcaOAntigo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarFluxo1 = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo 1", descricao = (string?)null, fluxoPadrao = false });
        var criarFluxo2 = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo 2", descricao = (string?)null, fluxoPadrao = true });
        var fluxo2Id = (await criarFluxo2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detalhe = await client.GetAsync($"/api/v1/tipos-processo/{tipoId}");
        var fluxos = (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fluxos").EnumerateArray().ToList();
        fluxos.Should().HaveCount(2);
        fluxos.Count(f => f.GetProperty("fluxoPadrao").GetBoolean()).Should().Be(1);
        fluxos.Single(f => f.GetProperty("id").GetGuid() == fluxo2Id).GetProperty("fluxoPadrao").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Fluxos_RemoverFluxoPadraoComOutrosExistindo_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var criarFluxo1 = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo padrão", descricao = (string?)null, fluxoPadrao = false });
        var fluxo1Id = (await criarFluxo1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo 2", descricao = (string?)null, fluxoPadrao = false });

        var resposta = await client.DeleteAsync($"/api/v1/fluxos/{fluxo1Id}");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Fluxos_DefinirFluxoPadraoExplicitamente_PromoveOFluxo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var (equipeId, _) = await CriarEquipeComMembroAsync(client);
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(equipeId));
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo 1", descricao = (string?)null, fluxoPadrao = false });
        var criarFluxo2 = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo 2", descricao = (string?)null, fluxoPadrao = false });
        var fluxo2Id = (await criarFluxo2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PostAsync($"/api/v1/fluxos/{fluxo2Id}/definir-padrao", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/tipos-processo/{tipoId}");
        var fluxos = (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("fluxos").EnumerateArray().ToList();
        fluxos.Count(f => f.GetProperty("fluxoPadrao").GetBoolean()).Should().Be(1);
        fluxos.Single(f => f.GetProperty("id").GetGuid() == fluxo2Id).GetProperty("fluxoPadrao").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task PostTiposProcesso_UsuarioAnalista_Retorna403()
    {
        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Teste", "analista-processos@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var resposta = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(Guid.NewGuid()));

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostTiposProcesso_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/v1/tipos-processo", NovoTipoProcessoPayload(Guid.NewGuid()));

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
