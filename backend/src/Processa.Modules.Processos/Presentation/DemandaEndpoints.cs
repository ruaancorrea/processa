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
            .WithTags("Demandas");

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
            .WithTags("Demandas");

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
            .WithTags("Demandas");

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
            .WithTags("Demandas");

        app.MapGet("/api/v1/demandas/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterDemandaPorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Demanda não encontrada.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterDemanda")
            .WithTags("Demandas");

        app.MapPost("/api/v1/demandas/{id:guid}/responsavel", async (Guid id, AtribuirResponsavelRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtribuirResponsavelDemandaCommand(id, request.ResponsavelId), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtribuirResponsavelDemanda")
            .WithTags("Demandas");

        app.MapPatch("/api/v1/demandas/{id:guid}/prioridade", async (Guid id, AtualizarPrioridadeRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarPrioridadeDemandaCommand(id, request.Prioridade), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarPrioridadeDemanda")
            .WithTags("Demandas");

        app.MapPost("/api/v1/demandas/{id:guid}/cancelar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CancelarDemandaCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CancelarDemanda")
            .WithTags("Demandas");

        // -------- Execução de etapas --------
        app.MapPost("/api/v1/execucoes-etapa/{id:guid}/concluir", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ConcluirExecucaoEtapaCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ConcluirExecucaoEtapa")
            .WithTags("Demandas");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/historico", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarHistoricoExecucaoQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarHistoricoExecucaoEtapa")
            .WithTags("Demandas");

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
            .WithTags("Demandas");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/comentarios", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarComentariosQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarComentarios")
            .WithTags("Demandas");

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
            .WithTags("Demandas");

        app.MapGet("/api/v1/execucoes-etapa/{id:guid}/anexos", async (Guid id, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarAnexosQuery(id), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarAnexos")
            .WithTags("Demandas");

        app.MapGet("/api/v1/anexos/{id:guid}/conteudo", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterConteudoAnexoQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.File(resultado.Value.Conteudo, resultado.Value.MimeType, resultado.Value.NomeOriginal)
                    : Results.Problem(title: "Anexo não encontrado.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("BaixarAnexo")
            .WithTags("Demandas");

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

public sealed record ValorCampoPersonalizadoDto(Guid CampoPersonalizadoId, string Valor);

public sealed record CriarDemandaRequest(
    Guid TipoProcessoId, Guid ClienteId, Guid? ResponsavelId, Prioridade Prioridade, DateTimeOffset? DataFimPrevista,
    List<ValorCampoPersonalizadoDto>? CamposPersonalizados);

public sealed record AtribuirResponsavelRequest(Guid ResponsavelId);

public sealed record AtualizarPrioridadeRequest(Prioridade Prioridade);

public sealed record AdicionarComentarioRequest(string? Texto);

public sealed record AtualizarDemandasEmMassaRequest(
    IReadOnlyList<Guid> DemandaIds, Guid? NovoResponsavelId, Prioridade? NovaPrioridade, bool? Cancelar);
