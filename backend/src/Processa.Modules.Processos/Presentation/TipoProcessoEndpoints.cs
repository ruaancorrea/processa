using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Processos.Application.Campos;
using Processa.Modules.Processos.Application.Fluxos;
using Processa.Modules.Processos.Application.TiposProcesso;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Presentation;

/// <summary>
/// Minimal API de configuração do motor de processos (Sprint 3). Nome de extensão
/// distinto de <see cref="TiposEtapaEndpoints.MapProcessosModule"/> — o mesmo módulo
/// registra duas fatias de rota independentes na composition root.
/// </summary>
public static class TipoProcessoEndpoints
{
    public static IEndpointRouteBuilder MapConfiguracaoProcessosModule(this IEndpointRouteBuilder app)
    {
        // -------- Tipos de processo --------
        app.MapPost("/api/v1/tipos-processo", async (CriarTipoProcessoRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarTipoProcessoCommand(
                    request.EquipeId, request.Nome, request.Descricao, request.ResponsavelObrigatorio,
                    request.ModoAtribuicao, request.ResponsavelFixoId), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/tipos-processo/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar o tipo de processo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarTipoProcesso")
            .WithTags("Processos");

        app.MapGet("/api/v1/tipos-processo", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarTiposProcessoQuery(), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarTiposProcesso")
            .WithTags("Processos");

        app.MapGet("/api/v1/tipos-processo/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterTipoProcessoPorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Tipo de processo não encontrado.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterTipoProcesso")
            .WithTags("Processos");

        app.MapPut("/api/v1/tipos-processo/{id:guid}", async (Guid id, AtualizarTipoProcessoRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarTipoProcessoCommand(
                    id, request.Nome, request.Descricao, request.ResponsavelObrigatorio,
                    request.ModoAtribuicao, request.ResponsavelFixoId), ct);
                return resultado.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(title: "Não foi possível atualizar o tipo de processo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarTipoProcesso")
            .WithTags("Processos");

        app.MapPost("/api/v1/tipos-processo/{id:guid}/ativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtivarTipoProcessoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtivarTipoProcesso")
            .WithTags("Processos");

        app.MapPost("/api/v1/tipos-processo/{id:guid}/desativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new DesativarTipoProcessoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("DesativarTipoProcesso")
            .WithTags("Processos");

        app.MapPut("/api/v1/tipos-processo/{id:guid}/permissoes-inicio",
                async (Guid id, DefinirPermissoesInicioRequest request, ISender sender, CancellationToken ct) =>
                {
                    var resultado = await sender.Send(new DefinirPermissoesInicioCommand(id, request.Perfis, request.UsuarioIds), ct);
                    return resultado.IsSuccess
                        ? Results.NoContent()
                        : Results.Problem(title: "Não foi possível definir as permissões de início.", detail: resultado.Error, statusCode: 422);
                })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("DefinirPermissoesInicioTipoProcesso")
            .WithTags("Processos");

        // -------- Campos personalizados --------
        app.MapPost("/api/v1/tipos-processo/{id:guid}/campos",
                async (Guid id, CriarCampoPersonalizadoRequest request, ISender sender, CancellationToken ct) =>
                {
                    var resultado = await sender.Send(new CriarCampoPersonalizadoCommand(
                        id, request.Nome, request.Tipo, request.Opcoes, request.Obrigatorio, request.Ordem), ct);
                    return resultado.IsSuccess
                        ? Results.Created($"/api/v1/tipos-processo/{id}/campos/{resultado.Value}", new { id = resultado.Value })
                        : Results.Problem(title: "Não foi possível criar o campo personalizado.", detail: resultado.Error, statusCode: 422);
                })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarCampoPersonalizado")
            .WithTags("Processos");

        app.MapPut("/api/v1/campos-personalizados/{id:guid}",
                async (Guid id, AtualizarCampoPersonalizadoRequest request, ISender sender, CancellationToken ct) =>
                {
                    var resultado = await sender.Send(
                        new AtualizarCampoPersonalizadoCommand(id, request.Nome, request.Opcoes, request.Obrigatorio, request.Ordem), ct);
                    return resultado.IsSuccess
                        ? Results.NoContent()
                        : Results.Problem(title: "Não foi possível atualizar o campo personalizado.", detail: resultado.Error, statusCode: 422);
                })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarCampoPersonalizado")
            .WithTags("Processos");

        app.MapDelete("/api/v1/campos-personalizados/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new RemoverCampoPersonalizadoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("RemoverCampoPersonalizado")
            .WithTags("Processos");

        // -------- Fluxos --------
        app.MapPost("/api/v1/tipos-processo/{id:guid}/fluxos", async (Guid id, CriarFluxoRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarFluxoCommand(id, request.Nome, request.Descricao, request.FluxoPadrao), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/tipos-processo/{id}/fluxos/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar o fluxo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarFluxo")
            .WithTags("Processos");

        app.MapPut("/api/v1/fluxos/{id:guid}", async (Guid id, AtualizarFluxoRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarFluxoCommand(id, request.Nome, request.Descricao), ct);
                return resultado.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(title: "Não foi possível atualizar o fluxo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarFluxo")
            .WithTags("Processos");

        app.MapPost("/api/v1/fluxos/{id:guid}/definir-padrao", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new DefinirFluxoPadraoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("DefinirFluxoPadrao")
            .WithTags("Processos");

        app.MapDelete("/api/v1/fluxos/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new RemoverFluxoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("RemoverFluxo")
            .WithTags("Processos");

        return app;
    }
}

public sealed record CriarTipoProcessoRequest(
    Guid EquipeId, string Nome, string? Descricao, bool ResponsavelObrigatorio, ModoAtribuicao ModoAtribuicao, Guid? ResponsavelFixoId);

public sealed record AtualizarTipoProcessoRequest(
    string Nome, string? Descricao, bool ResponsavelObrigatorio, ModoAtribuicao ModoAtribuicao, Guid? ResponsavelFixoId);

public sealed record DefinirPermissoesInicioRequest(List<Perfil>? Perfis, List<Guid>? UsuarioIds);

public sealed record CriarCampoPersonalizadoRequest(string Nome, TipoCampoPersonalizado Tipo, List<string>? Opcoes, bool Obrigatorio, int Ordem);

public sealed record AtualizarCampoPersonalizadoRequest(string Nome, List<string>? Opcoes, bool Obrigatorio, int Ordem);

public sealed record CriarFluxoRequest(string Nome, string? Descricao, bool FluxoPadrao);

public sealed record AtualizarFluxoRequest(string Nome, string? Descricao);
