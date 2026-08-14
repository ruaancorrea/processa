using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Processa.IntegrationTests.Identidade;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.IntegrationTests.Clientes;

[Collection("ProcessaApi")]
public class ClienteEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Clientes",
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

    private static object NovoClientePayload(string? cnpj = null) => new
    {
        razaoSocial = "Cliente Contábil LTDA",
        cnpj = cnpj ?? CnpjTestHelper.GerarValido(),
        codigoExterno = "COD-1",
        grupoClienteId = (Guid?)null,
        regimeTributario = "SimplesNacional",
        dataEntrada = "2026-01-01",
    };

    [Fact]
    public async Task PostClientes_DadosValidos_Retorna201()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());

        resposta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task PostClientes_CnpjInvalido_Retorna422ComErrosPorCampo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var resposta = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload("123"));

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Cnpj").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task AdicionarContato_SemNenhumMeioDeContato_Retorna422ComErrosPorCampo()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criarCliente = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());
        var clienteId = (await criarCliente.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.PostAsJsonAsync($"/api/v1/clientes/{clienteId}/contatos",
            new { nome = "Sem Contato", email = (string?)null, telefone = (string?)null, celular = (string?)null });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errors").GetProperty("Contato").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task PostClientes_CnpjDuplicadoNoMesmoTenant_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var cnpj = CnpjTestHelper.GerarValido();
        await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload(cnpj));

        var segunda = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload(cnpj));

        segunda.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetClientes_ListaClientesDoTenant()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());

        var resposta = await client.GetAsync("/api/v1/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task FluxoCompleto_ClienteComContatoEResponsavelPorEquipe()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var criarCliente = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());
        var clienteId = (await criarCliente.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var adicionarContato = await client.PostAsJsonAsync($"/api/v1/clientes/{clienteId}/contatos",
            new { nome = "Maria Silva", email = "maria@exemplo.com", telefone = (string?)null, celular = (string?)null });
        adicionarContato.StatusCode.Should().Be(HttpStatusCode.Created);

        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();
        await client.PostAsJsonAsync($"/api/v1/equipes/{equipeId}/membros", new { usuarioId, papel = "Gestor" });

        var adicionarResponsavel = await client.PostAsJsonAsync(
            $"/api/v1/clientes/{clienteId}/responsaveis", new { equipeId, usuarioId });
        adicionarResponsavel.StatusCode.Should().Be(HttpStatusCode.Created);

        var detalhe = await client.GetAsync($"/api/v1/clientes/{clienteId}");
        var detalheBody = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        detalheBody.GetProperty("contatos").GetArrayLength().Should().Be(1);
        detalheBody.GetProperty("responsaveis").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task AdicionarResponsavel_UsuarioNaoEhMembroDaEquipe_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criarCliente = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());
        var clienteId = (await criarCliente.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();
        // Nunca adiciona usuarioId como membro da equipe.

        var resposta = await client.PostAsJsonAsync($"/api/v1/clientes/{clienteId}/responsaveis", new { equipeId, usuarioId });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SuspenderInativarReativarCliente_FluxoCompleto()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criar = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());
        var clienteId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        (await client.PostAsync($"/api/v1/clientes/{clienteId}/suspender", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósSuspender = await client.GetAsync($"/api/v1/clientes/{clienteId}");
        (await apósSuspender.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Suspenso");

        (await client.PostAsync($"/api/v1/clientes/{clienteId}/reativar", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var apósReativar = await client.GetAsync($"/api/v1/clientes/{clienteId}");
        (await apósReativar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Ativo");
    }

    [Fact]
    public async Task PostGruposClientes_DadosValidos_Retorna201EAparecemNaListagem()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();

        var criar = await client.PostAsJsonAsync("/api/v1/grupos-clientes", new { nome = "Grupo Varejo", descricao = (string?)null });
        criar.StatusCode.Should().Be(HttpStatusCode.Created);

        var listar = await client.GetAsync("/api/v1/grupos-clientes");
        (await listar.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task PostClientes_UsuarioAnalista_Retorna403()
    {
        // Mesmo padrão do Sprint 1 (AdminPing_UsuarioNaoAdmin_Retorna403): não há
        // endpoint público pra criar usuário Analista ainda, então o token é emitido
        // direto pelo gerador real do módulo, provando que a policy GestorOuAdmin
        // bloqueia de verdade pelo middleware de autorização, não só por estar anotada.
        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Teste", "analista-clientes@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);

        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var resposta = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostClientes_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/v1/clientes", NovoClientePayload());

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
