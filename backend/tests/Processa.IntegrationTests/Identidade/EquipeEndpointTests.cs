using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Processa.IntegrationTests.Identidade;

using Processa.IntegrationTests;

[Collection("ProcessaApi")]
public class EquipeEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Equipes",
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

    [Fact]
    public async Task PostEquipes_DadosValidos_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = "Apuração de impostos" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostEquipes_NomeVazio_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "", descricao = (string?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetEquipes_ListaEquipesDoTenant()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });

        var resposta = await client.GetAsync("/api/v1/equipes");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetEquipePorId_EquipeExiste_RetornaComMembros()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criar = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.GetAsync($"/api/v1/equipes/{equipeId}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("nome").GetString().Should().Be("Equipe Fiscal");
        body.GetProperty("membros").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task PutEquipe_AtualizaNomeEDescricao()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criar = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Nome Antigo", descricao = (string?)null });
        var equipeId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PutAsJsonAsync($"/api/v1/equipes/{equipeId}", new { nome = "Nome Novo", descricao = "Atualizada" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var obtida = await client.GetAsync($"/api/v1/equipes/{equipeId}");
        (await obtida.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("nome").GetString().Should().Be("Nome Novo");
    }

    [Fact]
    public async Task DesativarEReativarEquipe_AlternaAtiva()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criar = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var desativar = await client.PostAsync($"/api/v1/equipes/{equipeId}/desativar", null);
        desativar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósDesativar = await client.GetAsync($"/api/v1/equipes/{equipeId}");
        (await apósDesativar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ativa").GetBoolean().Should().BeFalse();

        var reativar = await client.PostAsync($"/api/v1/equipes/{equipeId}/reativar", null);
        reativar.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósReativar = await client.GetAsync($"/api/v1/equipes/{equipeId}");
        (await apósReativar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ativa").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AdicionarERemoverMembro_FluxoCompleto()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // O único usuário existente no tenant, além do admin logado, é o próprio admin —
        // criar um 2º tenant/usuário não ajudaria (RBAC exige que o membro pertença ao
        // MESMO tenant de quem adiciona). Reaproveita o próprio admin como membro: o
        // domínio permite (só recusa Papel=Admin, não o próprio usuário-admin como membro
        // com papel Gestor/Analista).
        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();

        var adicionar = await client.PostAsJsonAsync($"/api/v1/equipes/{equipeId}/membros", new { usuarioId, papel = "Gestor" });
        adicionar.StatusCode.Should().Be(HttpStatusCode.Created);

        var detalhe = await client.GetAsync($"/api/v1/equipes/{equipeId}");
        (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("membros").GetArrayLength().Should().Be(1);

        var remover = await client.DeleteAsync($"/api/v1/equipes/{equipeId}/membros/{usuarioId}");
        remover.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var detalheApósRemover = await client.GetAsync($"/api/v1/equipes/{equipeId}");
        (await detalheApósRemover.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("membros").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task AdicionarMembro_PapelAdmin_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();

        var resposta = await client.PostAsJsonAsync($"/api/v1/equipes/{equipeId}/membros", new { usuarioId, papel = "Admin" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        // Achado real de revisão: essa regra vivia só no Domain, então a resposta não
        // trazia o dict "errors" documentado (mesma classe de bug do CNPJ no Sprint 1) —
        // agora também está no FluentValidation, então o contrato RFC 9457 é respeitado.
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Papel").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostEquipes_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
