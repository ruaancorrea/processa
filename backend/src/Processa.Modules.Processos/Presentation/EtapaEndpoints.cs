using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Processos.Application.Etapas;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Presentation;

/// <summary>
/// Minimal API das etapas de um fluxo (Sprint 4). Nome de extensão distinto de
/// <see cref="TipoProcessoEndpoints.MapConfiguracaoProcessosModule"/> e
/// <see cref="TiposEtapaEndpoints.MapProcessosModule"/> — o mesmo módulo registra
/// três fatias de rota independentes na composition root.
/// </summary>
public static class EtapaEndpoints
{
    public static IEndpointRouteBuilder MapEtapasProcessosModule(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/fluxos/{fluxoId:guid}/etapas", async (Guid fluxoId, CriarEtapaRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(
                    new CriarEtapaCommand(fluxoId, request.Nome, request.Descricao, request.Tipo, request.Ordem, request.Configuracao), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/etapas/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar a etapa.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarEtapa")
            .WithTags("Processos");

        app.MapGet("/api/v1/fluxos/{fluxoId:guid}/etapas", async (Guid fluxoId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarEtapasQuery(fluxoId), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarEtapas")
            .WithTags("Processos");

        app.MapGet("/api/v1/etapas/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterEtapaPorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Etapa não encontrada.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterEtapa")
            .WithTags("Processos");

        app.MapPut("/api/v1/etapas/{id:guid}", async (Guid id, AtualizarEtapaRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(
                    new AtualizarEtapaCommand(id, request.Nome, request.Descricao, request.Ordem, request.Configuracao), ct);
                return resultado.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(title: "Não foi possível atualizar a etapa.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarEtapa")
            .WithTags("Processos");

        app.MapDelete("/api/v1/etapas/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new RemoverEtapaCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("RemoverEtapa")
            .WithTags("Processos");

        app.MapPut("/api/v1/etapas/{id:guid}/configuracao-acesso",
                async (Guid id, DefinirConfiguracaoAcessoEtapaRequest request, ISender sender, CancellationToken ct) =>
                {
                    var resultado = await sender.Send(
                        new DefinirConfiguracaoAcessoEtapaCommand(id, request.PodeAlterar, request.UsuarioIdsPodeAlterar), ct);
                    return resultado.IsSuccess
                        ? Results.NoContent()
                        : Results.Problem(title: "Não foi possível definir a configuração de acesso.", detail: resultado.Error, statusCode: 422);
                })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("DefinirConfiguracaoAcessoEtapa")
            .WithTags("Processos");

        return app;
    }
}

public sealed record CriarEtapaRequest(string Nome, string? Descricao, TipoEtapa Tipo, int Ordem, ConfiguracaoEtapa? Configuracao);

public sealed record AtualizarEtapaRequest(string Nome, string? Descricao, int Ordem, ConfiguracaoEtapa? Configuracao);

public sealed record DefinirConfiguracaoAcessoEtapaRequest(List<Perfil>? PodeAlterar, List<Guid>? UsuarioIdsPodeAlterar);
