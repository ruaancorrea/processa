using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Processa.IntegrationTests.Identidade;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.IntegrationTests.Processos;

/// <summary>
/// Fim a fim contra Postgres real (Testcontainers) e MinIO real (docker-compose local —
/// ver docker/README): valida a integração que os testes unitários não conseguem, em
/// especial o round-trip de anexo pelo storage S3-compatível de verdade.
/// </summary>
[Collection("ProcessaApi")]
public class DemandaEndpointTests(ProcessaApiFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => fixture.LimparDadosAsync();

    private async Task<HttpClient> CriarClienteAutenticadoComoAdminAsync()
    {
        var client = fixture.CreateClient();
        var email = $"admin-{Guid.NewGuid():N}@exemplo.com";

        await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            nomeEscritorio = "Escritório Demandas",
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

    /// <summary>TipoProcesso Manual/não-obrigatório + fluxo PADRÃO com 2 etapas Comum em sequência (ordem 0 e 1).</summary>
    private static async Task<Guid> CriarTipoProcessoComFluxoLinearAsync(HttpClient client)
    {
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe Fiscal", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", new
        {
            equipeId,
            nome = "Abertura de empresa",
            descricao = (string?)null,
            responsavelObrigatorio = false,
            modoAtribuicao = "Manual",
            responsavelFixoId = (Guid?)null,
        });
        var tipoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var criarFluxo = await client.PostAsJsonAsync($"/api/v1/tipos-processo/{tipoId}/fluxos",
            new { nome = "Fluxo padrão", descricao = (string?)null, fluxoPadrao = true });
        var fluxoId = (await criarFluxo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Primeira", descricao = (string?)null, tipo = "Comum", ordem = 0, configuracao = (object?)null });
        await client.PostAsJsonAsync($"/api/v1/fluxos/{fluxoId}/etapas",
            new { nome = "Segunda", descricao = (string?)null, tipo = "Comum", ordem = 1, configuracao = (object?)null });

        return tipoId;
    }

    private static async Task<Guid> CriarClienteAsync(HttpClient client)
    {
        var resposta = await client.PostAsJsonAsync("/api/v1/clientes", new
        {
            razaoSocial = "Cliente Contábil LTDA",
            cnpj = CnpjTestHelper.GerarValido(),
            codigoExterno = "COD-1",
            grupoClienteId = (Guid?)null,
            regimeTributario = "SimplesNacional",
            dataEntrada = "2026-01-01",
        });
        return (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<(Guid demandaId, Guid execucaoAtualId)> AbrirDemandaAsync(HttpClient client, Guid tipoProcessoId, Guid clienteId)
    {
        var criar = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            tipoProcessoId,
            clienteId,
            responsavelId = (Guid?)null,
            prioridade = "Media",
            dataFimPrevista = (DateTimeOffset?)null,
            camposPersonalizados = (object?)null,
        });
        criar.StatusCode.Should().Be(HttpStatusCode.Created);
        var demandaId = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        var execucaoAtualId = (await detalhe.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("execucoes")[0].GetProperty("id").GetGuid();

        return (demandaId, execucaoAtualId);
    }

    [Fact]
    public async Task PostDemandas_ClienteInativoOuInexistente_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);

        var resposta = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            tipoProcessoId,
            clienteId = Guid.NewGuid(),
            responsavelId = (Guid?)null,
            prioridade = "Media",
            dataFimPrevista = (DateTimeOffset?)null,
            camposPersonalizados = (object?)null,
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task PostDemandas_DadosValidos_Retorna201EOrquestradorAbrePrimeiraExecucao()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);

        var (demandaId, _) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("SemResponsavel");
        var execucoes = body.GetProperty("execucoes").EnumerateArray().ToList();
        execucoes.Should().HaveCount(1);
        // AvancarRamoAsync chama execucao.Iniciar() assim que a cria — nunca fica "Pendente" visível pela API.
        execucoes[0].GetProperty("status").GetString().Should().Be("EmAndamento");
    }

    [Fact]
    public async Task ConcluirExecucao_EtapaComumLinear_AvancaParaProximaEtapaEMantemHistorico()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (demandaId, execucaoAtualId) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var concluir = await client.PostAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/concluir", null);

        concluir.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        var execucoes = (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("execucoes").EnumerateArray().ToList();
        execucoes.Should().HaveCount(2);
        execucoes.Should().ContainSingle(e => e.GetProperty("status").GetString() == "Concluida");
        execucoes.Should().ContainSingle(e => e.GetProperty("status").GetString() == "EmAndamento");

        var historico = await client.GetAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/historico");
        (await historico.Content.ReadFromJsonAsync<JsonElement>()).GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Comentarios_AdicionarEListar_PersisteNoPostgresReal()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (_, execucaoAtualId) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var adicionar = await client.PostAsJsonAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/comentarios", new { texto = "Aguardando documentação do cliente." });
        adicionar.StatusCode.Should().Be(HttpStatusCode.Created);

        var listar = await client.GetAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/comentarios");
        var comentarios = (await listar.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        comentarios.Should().ContainSingle(c => c.GetProperty("texto").GetString() == "Aguardando documentação do cliente.");
    }

    [Fact]
    public async Task Comentarios_TextoVazio_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (_, execucaoAtualId) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var resposta = await client.PostAsJsonAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/comentarios", new { texto = "" });

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Anexos_EnviarELerConteudo_RoundTripPeloMinioReal()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (_, execucaoAtualId) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);
        var conteudoOriginal = Encoding.UTF8.GetBytes("Conteúdo real do anexo de teste — round-trip via MinIO.");

        using var form = new MultipartFormDataContent();
        using var arquivoContent = new ByteArrayContent(conteudoOriginal);
        arquivoContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(arquivoContent, "arquivo", "documento.txt");

        var enviar = await client.PostAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/anexos", form);
        enviar.StatusCode.Should().Be(HttpStatusCode.Created);
        var anexoId = (await enviar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var listar = await client.GetAsync($"/api/v1/execucoes-etapa/{execucaoAtualId}/anexos");
        var anexos = (await listar.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        anexos.Should().ContainSingle(a => a.GetProperty("nomeOriginal").GetString() == "documento.txt");

        var baixar = await client.GetAsync($"/api/v1/anexos/{anexoId}/conteudo");
        baixar.StatusCode.Should().Be(HttpStatusCode.OK);
        var conteudoBaixado = await baixar.Content.ReadAsByteArrayAsync();
        conteudoBaixado.Should().Equal(conteudoOriginal);
    }

    [Fact]
    public async Task AtribuirResponsavel_DemandaSemResponsavel_AtualizaStatusParaPendente()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (demandaId, _) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);
        var me = await client.GetAsync("/api/v1/me");
        var usuarioId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetGuid();

        var resposta = await client.PostAsJsonAsync($"/api/v1/demandas/{demandaId}/responsavel", new { responsavelId = usuarioId });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        var body = await detalhe.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Pendente");
        body.GetProperty("responsavelId").GetGuid().Should().Be(usuarioId);
    }

    [Fact]
    public async Task AtualizarPrioridade_DadosValidos_Retorna204EReflete()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (demandaId, _) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var resposta = await client.PatchAsJsonAsync($"/api/v1/demandas/{demandaId}/prioridade", new { prioridade = "Urgente" });

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("prioridade").GetString().Should().Be("Urgente");
    }

    [Fact]
    public async Task CancelarDemanda_DemandaAberta_Retorna204EStatusCancelado()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var (demandaId, _) = await AbrirDemandaAsync(client, tipoProcessoId, clienteId);

        var resposta = await client.PostAsync($"/api/v1/demandas/{demandaId}/cancelar", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var detalhe = await client.GetAsync($"/api/v1/demandas/{demandaId}");
        (await detalhe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("status").GetString().Should().Be("Cancelado");
    }

    [Fact]
    public async Task CancelarDemanda_UsuarioAnalista_Retorna403()
    {
        var adminClient = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(adminClient);
        var clienteId = await CriarClienteAsync(adminClient);
        var (demandaId, _) = await AbrirDemandaAsync(adminClient, tipoProcessoId, clienteId);

        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Teste", "analista-demandas@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);
        var analistaClient = fixture.CreateClient();
        analistaClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var resposta = await analistaClient.PostAsync($"/api/v1/demandas/{demandaId}/cancelar", null);

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostDemandas_SemToken_Retorna401()
    {
        var client = fixture.CreateClient();

        var resposta = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            tipoProcessoId = Guid.NewGuid(),
            clienteId = Guid.NewGuid(),
            responsavelId = (Guid?)null,
            prioridade = "Media",
            dataFimPrevista = (DateTimeOffset?)null,
            camposPersonalizados = (object?)null,
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ===================== PROJ-57/58/59 (Sprint 6) =====================

    private static async Task<Guid> AbrirDemandaComPrioridadeAsync(HttpClient client, Guid tipoProcessoId, Guid clienteId, string prioridade)
    {
        var criar = await client.PostAsJsonAsync("/api/v1/demandas", new
        {
            tipoProcessoId,
            clienteId,
            responsavelId = (Guid?)null,
            prioridade,
            dataFimPrevista = (DateTimeOffset?)null,
            camposPersonalizados = (object?)null,
        });
        return (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    [Fact]
    public async Task GetKanban_TipoProcessoValido_RetornaColunasNaOrdemDasEtapasECardNaColunaCerta()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Alta");

        var resposta = await client.GetAsync($"/api/v1/demandas/kanban?tipoProcessoId={tipoProcessoId}");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        var colunas = body.GetProperty("colunas").EnumerateArray().ToList();
        colunas.Should().HaveCount(2);
        colunas[0].GetProperty("etapaNome").GetString().Should().Be("Primeira");
        colunas[1].GetProperty("etapaNome").GetString().Should().Be("Segunda");
        var cards = body.GetProperty("cards").EnumerateArray().ToList();
        cards.Should().ContainSingle(c => c.GetProperty("etapaAtualId").GetGuid() == colunas[0].GetProperty("etapaId").GetGuid());
    }

    [Fact]
    public async Task GetKanban_TipoProcessoSemFluxoPadrao_Retorna422()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var criarEquipe = await client.PostAsJsonAsync("/api/v1/equipes", new { nome = "Equipe sem fluxo", descricao = (string?)null });
        var equipeId = (await criarEquipe.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var criarTipo = await client.PostAsJsonAsync("/api/v1/tipos-processo", new
        {
            equipeId,
            nome = "Sem fluxo",
            descricao = (string?)null,
            responsavelObrigatorio = false,
            modoAtribuicao = "Manual",
            responsavelFixoId = (Guid?)null,
        });
        var tipoProcessoId = (await criarTipo.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var resposta = await client.GetAsync($"/api/v1/demandas/kanban?tipoProcessoId={tipoProcessoId}");

        resposta.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetDemandas_OrdenacaoPorPrioridadeDescendente_RespeitaSeveridadeNaoOrdemAlfabetica()
    {
        // Alfabeticamente (o que a coluna string daria sem o mapeamento de rank): Urgente,
        // Media, Baixa, Alta. Por severidade (o correto): Urgente, Alta, Media, Baixa.
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Baixa");
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Media");
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Urgente");
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Alta");

        var resposta = await client.GetAsync("/api/v1/demandas?ordenarPor=Prioridade:desc");

        var itens = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("itens").EnumerateArray().ToList();
        itens.Select(i => i.GetProperty("prioridade").GetString()).Should().Equal("Urgente", "Alta", "Media", "Baixa");
    }

    [Fact]
    public async Task GetDemandas_FiltroPorPrioridade_RetornaSoAsCorrespondentes()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Baixa");
        await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Urgente");

        var resposta = await client.GetAsync("/api/v1/demandas?prioridade=Urgente");

        var itens = (await resposta.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("itens").EnumerateArray().ToList();
        itens.Should().ContainSingle();
        itens[0].GetProperty("prioridade").GetString().Should().Be("Urgente");
    }

    [Fact]
    public async Task GetDemandas_Paginacao_RespeitaTamanhoERetornaTotalCorreto()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        for (var i = 0; i < 3; i++)
            await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Media");

        var resposta = await client.GetAsync("/api/v1/demandas?pagina=1&tamanhoPagina=2");

        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("itens").GetArrayLength().Should().Be(2);
        body.GetProperty("totalRegistros").GetInt32().Should().Be(3);
        body.GetProperty("totalPaginas").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task PatchBulk_AlteraPrioridadeDeVariasDemandas_Retorna200ComContagemDeSucesso()
    {
        var client = await CriarClienteAutenticadoComoAdminAsync();
        var tipoProcessoId = await CriarTipoProcessoComFluxoLinearAsync(client);
        var clienteId = await CriarClienteAsync(client);
        var id1 = await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Baixa");
        var id2 = await AbrirDemandaComPrioridadeAsync(client, tipoProcessoId, clienteId, "Baixa");

        var resposta = await client.PatchAsJsonAsync("/api/v1/demandas/bulk", new
        {
            demandaIds = new[] { id1, id2 },
            novoResponsavelId = (Guid?)null,
            novaPrioridade = "Urgente",
            cancelar = (bool?)null,
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resposta.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("totalSucesso").GetInt32().Should().Be(2);
        var detalhe1 = await client.GetAsync($"/api/v1/demandas/{id1}");
        (await detalhe1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("prioridade").GetString().Should().Be("Urgente");
    }

    [Fact]
    public async Task PatchBulk_UsuarioAnalista_Retorna403()
    {
        using var scope = fixture.Services.CreateScope();
        var tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        var usuarioAnalista = Usuario.Criar(Guid.NewGuid(), "Analista Bulk", "analista-bulk@exemplo.com", "hash", Perfil.Analista).Value;
        var token = tokenGenerator.GerarAccessToken(usuarioAnalista);
        var analistaClient = fixture.CreateClient();
        analistaClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

        var resposta = await analistaClient.PatchAsJsonAsync("/api/v1/demandas/bulk", new
        {
            demandaIds = new[] { Guid.NewGuid() },
            novoResponsavelId = (Guid?)null,
            novaPrioridade = "Urgente",
            cancelar = (bool?)null,
        });

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
