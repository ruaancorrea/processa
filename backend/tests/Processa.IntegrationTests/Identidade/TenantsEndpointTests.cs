using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Processa.IntegrationTests.Identidade;

[Collection("Identidade")]
public class TenantsEndpointTests(IdentidadeApiFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _client = fixture.CreateClient();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    [Fact]
    public async Task PostTenants_DadosValidos_Retorna201ComIds()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Integração",
            cnpj = CnpjTestHelper.GerarValido(),
            nomeAdmin = "Admin Teste",
            emailAdmin = $"admin-{Guid.NewGuid():N}@exemplo.com",
            senha = "senhaForte123",
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("tenantId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("usuarioAdminId").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostTenants_CnpjInvalido_Retorna422NoFormatoRfc9457()
    {
        // Cobre a pendência do Sprint 0 (find-bugs): middleware global de exceção
        // (ValidationException -> 422 Problem Details) testado de ponta a ponta de verdade.
        var response = await _client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Inválido",
            cnpj = "123",
            nomeAdmin = "Admin",
            emailAdmin = $"admin-{Guid.NewGuid():N}@exemplo.com",
            senha = "senhaForte123",
        });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Cnpj").GetArrayLength().Should().BeGreaterThan(0);
        body.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task PostTenants_CnpjJaCadastrado_Retorna422()
    {
        var cnpj = CnpjTestHelper.GerarValido();
        var payload = new
        {
            nomeEscritorio = "Escritório Duplicado",
            cnpj,
            nomeAdmin = "Admin",
            emailAdmin = $"admin-{Guid.NewGuid():N}@exemplo.com",
            senha = "senhaForte123",
        };

        await _client.PostAsJsonAsync("/api/v1/tenants", payload);
        var segundaResposta = await _client.PostAsJsonAsync("/api/v1/tenants", payload with { emailAdmin = $"outro-{Guid.NewGuid():N}@exemplo.com" });

        segundaResposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostTenants_MesmoCnpjEmRequisicoesConcorrentes_UmaGanha422EmVezDe500()
    {
        // Corrida real: duas requisições concorrentes passam pela checagem de unicidade
        // da Application layer antes de qualquer uma commitar; a constraint UNIQUE do
        // Postgres barra a segunda no INSERT. Sem o tratamento de DbUpdateException no
        // GlobalExceptionHandler, essa corrida vazava como 500 genérico em vez do 422
        // "já existe" (achado real de revisão de segurança/robustez).
        var cnpj = CnpjTestHelper.GerarValido();
        var payloadA = new
        {
            nomeEscritorio = "Escritório Corrida A",
            cnpj,
            nomeAdmin = "Admin A",
            emailAdmin = $"corrida-a-{Guid.NewGuid():N}@exemplo.com",
            senha = "senhaForte123",
        };
        var payloadB = payloadA with
        {
            nomeEscritorio = "Escritório Corrida B",
            emailAdmin = $"corrida-b-{Guid.NewGuid():N}@exemplo.com",
        };

        var clienteA = fixture.CreateClient();
        var clienteB = fixture.CreateClient();

        var tarefaA = clienteA.PostAsJsonAsync("/api/v1/tenants", payloadA);
        var tarefaB = clienteB.PostAsJsonAsync("/api/v1/tenants", payloadB);
        var respostas = await Task.WhenAll(tarefaA, tarefaB);

        respostas.Should().ContainSingle(r => r.StatusCode == HttpStatusCode.Created);
        respostas.Should().ContainSingle(r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        respostas.Should().NotContain(r => (int)r.StatusCode >= 500);
    }
}
