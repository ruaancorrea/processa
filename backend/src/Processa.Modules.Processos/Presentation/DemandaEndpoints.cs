using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Presentation;

/// <summary>
/// Minimal API de execução de demandas (Sprint 5) e painel operacional/kanban (Sprint 6).
/// Nome de extensão distinto dos outros três já registrados pelo módulo Processos na
/// composition root.
/// </summary>
public static class DemandaEndpoints
{
    public static IEndpointRouteBuilder MapDemandasProcessosModule(this IEndpointRouteBuilder app)
    {
        // -------- Demandas --------
        app.MapPost("/api/v1/demandas", async (CriarDemandaRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarDemandaCommand(
                    request.TipoProcessoId, request.ClienteId, request.ResponsavelId, request.Prioridade, request.DataFimPrevista,
                    request.CamposPersonalizados?.Select(c => new ValorCampoPersonalizadoRequest(c.CampoPersonalizadoId, c.Valor)).ToList()), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/demandas/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível abrir a demanda.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("CriarDemanda")
            .WithTags("Demandas")
            .WithSummary("Abre uma nova demanda")
            .WithDescription(
                "Dispara o motor de execução: instancia o fluxo padrão do tipo de processo e inicia a "
                + "primeira etapa (ou o primeiro ramo, se o fluxo começar em fork). 422 se o tipo de "
                + "processo não tiver fluxo padrão configurado.");

        // PROJ-58: filtro/ordenação/paginação via query string. ordenarPor é repetível
        // ("?ordenarPor=Prioridade:desc&ordenarPor=DataInicio:asc") — ordenação cumulativa,
        // aplicada na ordem em que aparece. Analista só vê a própria fila mesmo se pedir
        // responsavelId de outra pessoa — o handler ignora e força (ver ListarDemandasQuery).
        app.MapGet("/api/v1/demandas", async (
                StatusDemanda? status, Guid? responsavelId, bool? semResponsavel, Prioridade? prioridade, Guid? clienteId,
                Guid? tipoProcessoId, Guid? etapaAtualId, DateTimeOffset? dataInicioDe, DateTimeOffset? dataInicioAte,
                DateTimeOffset? dataFimPrevistaDe, DateTimeOffset? dataFimPrevistaAte, string[]? ordenarPor,
                int? pagina, int? tamanhoPagina, ISender sender, CancellationToken ct) =>
            {
                var filtro = new FiltroDemandas(
                    status, responsavelId, semResponsavel, prioridade, clienteId, tipoProcessoId, etapaAtualId,
                    dataInicioDe, dataInicioAte, dataFimPrevistaDe, dataFimPrevistaAte);
                var ordenacao = ParsearOrdenacao(ordenarPor);
                var resultado = await sender.Send(new ListarDemandasQuery(filtro, ordenacao, pagina ?? 1, tamanhoPagina ?? 50), ct);
                return Results.Ok(resultado);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarDemandas")
            .WithTags("Demandas")
            .WithSummary("Lista demandas com filtro, ordenação cumulativa e paginação")
            .WithDescription(
                "Analista só vê a própria fila (responsavelId é forçado pelo handler, ignora o que vier "
                + "na query). ordenarPor é repetível — cada ocorrência é um critério aplicado na ordem "
                + "informada (\"?ordenarPor=Prioridade:desc&ordenarPor=DataInicio:asc\").");

        // PROJ-57: board kanban — coluna = Etapa do fluxo padrão do TipoProcesso.
        app.MapGet("/api/v1/demandas/kanban", async (Guid tipoProcessoId, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterKanbanQuery(tipoProcessoId), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Não foi possível montar o quadro.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterKanban")
            .WithTags("Demandas")
            .WithSummary("Monta o quadro kanban do tipo de processo")
            .WithDescription(
                "Colunas derivadas das etapas do fluxo padrão do tipo de processo, ordenadas por Ordem — "
                + "não existe configuração de coluna separada, o quadro segue o desenho do fluxo. "
                + "422 se o tipo de processo não tiver fluxo padrão. Mudança de coluna no board real é "
                + "sinalizada em tempo real via SignalR (/hubs/kanban, evento quadroAlterado).");

        // PROJ-59: edição em massa — melhor-esforço (ver AtualizarDemandasEmMassaCommand).
        app.MapPatch("/api/v1/demandas/bulk", async (AtualizarDemandasEmMassaRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarDemandasEmMassaCommand(
                    request.DemandaIds, request.NovoResponsavelId, request.NovaPrioridade, request.Cancelar ?? false), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Não foi possível atualizar as demandas selecionadas.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarDemandasEmMassa")
            .WithTags("Demandas")
            .WithSummary("Edita responsável, prioridade ou cancela várias demandas de uma vez")
            .WithDescription(
                "Melhor-esforço, não é atômico: cada demanda é processada isoladamente e a resposta "
                + "traz sucesso/falha por item, não um tudo-ou-nada. Uma demanda com RBAC insuficiente "
                + "ou estado inválido não derruba as demais.");

        app.MapGet("/api/v1/demandas/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterDemandaPorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Demanda não encontrada.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterDemanda")
            .WithTags("Demandas")
            .WithSummary("Detalhe completo de uma demanda");

        app.MapPost("/api/v1/demandas/{id:guid}/responsavel", async (Guid id, AtribuirResponsavelRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtribuirResponsavelDemandaCommand(id, request.ResponsavelId), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtribuirResponsavelDemanda")
            .WithTags("Demandas")
            .WithSummary("Atribui ou troca o responsável pela demanda");

        app.MapPatch("/api/v1/demandas/{id:guid}/prioridade", async (Guid id, AtualizarPrioridadeRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarPrioridadeDemandaCommand(id, request.Prioridade), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarPrioridadeDemanda")
            .WithTags("Demandas")
            .WithSummary("Altera a prioridade da demanda");

        app.MapPost("/api/v1/demandas/{id:guid}/cancelar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CancelarDemandaCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CancelarDemanda")
            .WithTags("Demandas")
            .WithSummary("Cancela a demanda")
            .WithDescription("Encerra a demanda e a execução da etapa atual sem concluí-la. Irreversível — não existe reabertura.");

        // -------- Execução de etapas --------
        app.MapPost("/api/v1/execucoes-etapa/{id:guid}/concluir", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ConcluirExecucaoEtapaCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ConcluirExecucaoEtapa")
            .WithTags("Demandas")
            .WithSummary("Conclui a execução da etapa atual")
            .WithDescription(
                "O coração do motor: o orquestrador decide o próximo passo sozinho a partir do tipo da "
                + "etapa concluída — avança pra próxima etapa Comum, dispara todos os ramos de um fork, "
                + "ou aguarda os demais ramos convergirem num join antes de liberar a etapa de União. "
                + "Quem pode concluir é decidido pela configuração de acesso da própria etapa "
                + "(PUT /api/v1/etapas/{id}/configuracao-acesso), não é um RBAC fixo do endpoint.");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/historico", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarHistoricoExecucaoQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarHistoricoExecucaoEtapa")
            .WithTags("Demandas")
            .WithSummary("Timeline completa da execução da etapa");

        // -------- Comentários --------
        app.MapPost("/api/v1/execucoes-etapa/{id:guid}/comentarios", async (Guid id, AdicionarComentarioRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AdicionarComentarioCommand(id, request.Texto), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/execucoes-etapa/{id}/comentarios/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível adicionar o comentário.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("AdicionarComentario")
            .WithTags("Demandas")
            .WithSummary("Adiciona comentário na execução da etapa");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/comentarios", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarComentariosQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarComentarios")
            .WithTags("Demandas")
            .WithSummary("Lista os comentários da execução da etapa");

        // -------- Anexos --------
        app.MapPost("/api/v1/execucoes-etapa/{id:guid}/anexos", async (Guid id, IFormFile arquivo, ISender sender, CancellationToken ct) =>
            {
                await using var conteudo = arquivo.OpenReadStream();
                var resultado = await sender.Send(
                    new AdicionarAnexoCommand(id, arquivo.FileName, conteudo, arquivo.Length, arquivo.ContentType), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/execucoes-etapa/{id}/anexos/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível anexar o arquivo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .DisableAntiforgery()
            .WithName("AdicionarAnexo")
            .WithTags("Demandas")
            .WithSummary("Envia um anexo (multipart/form-data) pra execução da etapa")
            .WithDescription("Armazenado em object storage (MinIO/S3-compatible), nunca no banco. Limite de 25 MB por arquivo.");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/anexos", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarAnexosQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarAnexos")
            .WithTags("Demandas")
            .WithSummary("Lista os anexos da execução da etapa");

        app.MapGet("/api/v1/anexos/{id:guid}/conteudo", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterConteudoAnexoQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.File(resultado.Value.Conteudo, resultado.Value.MimeType, resultado.Value.NomeOriginal)
                    : Results.Problem(title: "Anexo não encontrado.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("BaixarAnexo")
            .WithTags("Demandas")
            .WithSummary("Baixa o binário do anexo");

        return app;
    }

    private static List<OrdenacaoDemanda> ParsearOrdenacao(string[]? ordenarPor)
    {
        if (ordenarPor is null or { Length: 0 })
            return [];

        var resultado = new List<OrdenacaoDemanda>();
        foreach (var entrada in ordenarPor)
        {
            var partes = entrada.Split(':', 2, StringSplitOptions.TrimEntries);
            if (!Enum.TryParse<CampoOrdenacaoDemanda>(partes[0], ignoreCase: true, out var campo))
                continue;

            var descendente = partes.Length > 1 && partes[1].Equals("desc", StringComparison.OrdinalIgnoreCase);
            resultado.Add(new OrdenacaoDemanda(campo, descendente));
        }

        return resultado;
    }
}

/// <summary>Valor de um campo personalizado do tipo de processo, informado na abertura da demanda.</summary>
public sealed record ValorCampoPersonalizadoDto(Guid CampoPersonalizadoId, string Valor);

/// <summary>Payload de abertura de demanda — dispara o motor de execução a partir do fluxo padrão do tipo de processo.</summary>
public sealed record CriarDemandaRequest(
    Guid TipoProcessoId, Guid ClienteId, Guid? ResponsavelId, Prioridade Prioridade, DateTimeOffset? DataFimPrevista,
    List<ValorCampoPersonalizadoDto>? CamposPersonalizados);

/// <summary>Novo responsável pela demanda.</summary>
public sealed record AtribuirResponsavelRequest(Guid ResponsavelId);

/// <summary>Nova prioridade da demanda.</summary>
public sealed record AtualizarPrioridadeRequest(Prioridade Prioridade);

/// <summary>Texto do comentário — obrigatório e não-vazio (validado por FluentValidation).</summary>
public sealed record AdicionarComentarioRequest(string? Texto);

/// <summary>
/// Edição em massa — melhor-esforço, não atômica. Cada campo não-nulo é aplicado a todas as
/// demandas em <see cref="DemandaIds"/>; <see cref="Cancelar"/> true cancela em vez de editar.
/// </summary>
public sealed record AtualizarDemandasEmMassaRequest(
    IReadOnlyList<Guid> DemandaIds, Guid? NovoResponsavelId, Prioridade? NovaPrioridade, bool? Cancelar);
