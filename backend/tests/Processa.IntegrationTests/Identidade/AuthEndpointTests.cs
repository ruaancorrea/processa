using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.IntegrationTests.Identidade;

using Processa.IntegrationTests;

[Collection("ProcessaApi")]
public class AuthEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    private const string Senha = "senhaForte123";

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<(HttpClient Client, string Email)> CriarTenantEClienteAsync()
    {
        var client = fixture.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        var resposta = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Auth",
            cnpj = CnpjTestHelper.GerarValido(),
            nomeAdmin = "Admin Teste",
            emailAdmin = email,
            senha = Senha,
        });
        resposta.EnsureSuccessStatusCode();

        return (client, email);
    }

    [Fact]
    public async Task Login_CredenciaisCorretas_Retorna200ComAccessTokenESetaCookieDeRefresh()
    {
        var (client, email) = await CriarTenantEClienteAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        resposta.Headers.Should().Contain(h => h.Key == "Set-Cookie" && h.Value.Any(v => v.Contains("processa_refresh")));
    }

    [Fact]
    public async Task Login_SenhaErrada_Retorna401()
    {
        var (client, email) = await CriarTenantEClienteAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "senha-errada" });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_CincoTentativasErradasSeguidas_BloqueiaNaSextaMesmoComSenhaCerta()
    {
        var (client, email) = await CriarTenantEClienteAsync();

        for (var i = 0; i < 5; i++)
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = "senha-errada" });

        var respostaComSenhaCerta = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });

        respostaComSenhaCerta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var body = await respostaComSenhaCerta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("detail").GetString().Should().Contain("bloqueada");
    }

    [Fact]
    public async Task Refresh_ComCookieValido_RetornaNovoAccessTokenERotacionaOCookie()
    {
        var (client, email) = await CriarTenantEClienteAsync();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var primeiroAccessToken = loginBody.GetProperty("accessToken").GetString();

        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", null);

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshBody = await refreshResponse.Content.ReadFromJsonAsync<JsonElement>();
        refreshBody.GetProperty("accessToken").GetString().Should().NotBe(primeiroAccessToken);
    }

    [Fact]
    public async Task Refresh_SemCookie_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsync("/api/v1/auth/refresh", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.GetAsync("/api/v1/me");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ComTokenValido_Retorna200ComOsDadosDoUsuario()
    {
        var (client, email) = await CriarTenantEClienteAsync();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });
        var loginBody = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = loginBody.GetProperty("accessToken").GetString();
        var usuarioIdEsperado = loginBody.GetProperty("usuarioId").GetGuid();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resposta = await client.GetAsync("/api/v1/me");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("perfil").GetString().Should().Be("Admin");
        // Cobre um bug real pego em verificação manual: por padrão o JwtSecurityTokenHandler
        // renomeia a claim "sub" para ClaimTypes.NameIdentifier na validação, e FindFirst("sub")
        // no endpoint /me voltava null mesmo com token válido (ver MapInboundClaims em Program.cs).
        body.GetProperty("usuarioId").GetGuid().Should().Be(usuarioIdEsperado);
    }

    [Fact]
    public async Task AdminPing_UsuarioAdmin_Retorna200()
    {
        // O usuário criado junto com o tenant nasce com perfil Admin (ver CriarTenantCommandHandler).
        var (client, email) = await CriarTenantEClienteAsync();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, senha = Senha });
        var accessToken = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var resposta = await client.GetAsync("/api/v1/admin/ping");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminPing_UsuarioNaoAdmin_Retorna403()
    {
        // Sprint 1 ainda não tem endpoint público para criar usuário com perfil
        // diferente de Admin (isso nasce com o módulo Clientes/Equipes, Sprint 2+) —
        // o token é emitido diretamente pelo gerador real do módulo para provar que a
        // policy de autorização em si diferencia papéis corretamente, ponta a ponta
        // através do middleware de autorização de verdade, não só em unit test.
        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Teste", "analista@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        var resposta = await client.GetAsync("/api/v1/admin/ping");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
