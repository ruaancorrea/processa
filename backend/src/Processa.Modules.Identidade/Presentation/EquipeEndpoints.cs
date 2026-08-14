using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Identidade.Application.Equipes;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Presentation;

public static class EquipeEndpoints
{
    public static IEndpointRouteBuilder MapEquipesModule(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/equipes", async (CriarEquipeRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarEquipeCommand(request.Nome, request.Descricao), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/equipes/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar a equipe.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("CriarEquipe")
            .WithTags("Equipes");

        app.MapGet("/api/v1/equipes", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarEquipesQuery(), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarEquipes")
            .WithTags("Equipes");

        app.MapGet("/api/v1/equipes/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterEquipePorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Equipe não encontrada.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterEquipe")
            .WithTags("Equipes");

        app.MapPut("/api/v1/equipes/{id:guid}", async (Guid id, AtualizarEquipeRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarEquipeCommand(id, request.Nome, request.Descricao), ct);
                return resultado.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(title: "Não foi possível atualizar a equipe.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("AtualizarEquipe")
            .WithTags("Equipes");

        app.MapPost("/api/v1/equipes/{id:guid}/desativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new DesativarEquipeCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("DesativarEquipe")
            .WithTags("Equipes");

        app.MapPost("/api/v1/equipes/{id:guid}/reativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ReativarEquipeCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("ReativarEquipe")
            .WithTags("Equipes");

        app.MapPost("/api/v1/equipes/{id:guid}/membros", async (Guid id, AdicionarMembroRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AdicionarMembroCommand(id, request.UsuarioId, request.Papel), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/equipes/{id}/membros/{request.UsuarioId}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível adicionar o membro.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("AdicionarMembroEquipe")
            .WithTags("Equipes");

        app.MapDelete("/api/v1/equipes/{id:guid}/membros/{usuarioId:guid}", async (Guid id, Guid usuarioId, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new RemoverMembroCommand(id, usuarioId), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("Admin")
            .WithName("RemoverMembroEquipe")
            .WithTags("Equipes");

        return app;
    }
}

public sealed record CriarEquipeRequest(string Nome, string? Descricao);

public sealed record AtualizarEquipeRequest(string Nome, string? Descricao);

public sealed record AdicionarMembroRequest(Guid UsuarioId, Perfil Papel);
