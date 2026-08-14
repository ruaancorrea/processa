using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Clientes.Application.Clientes;
using Processa.Modules.Clientes.Application.Contatos;
using Processa.Modules.Clientes.Application.Grupos;
using Processa.Modules.Clientes.Application.Responsaveis;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Presentation;

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClientesModule(this IEndpointRouteBuilder app)
    {
        // -------- Grupos de clientes --------
        app.MapPost("/api/v1/grupos-clientes", async (CriarGrupoClienteRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarGrupoClienteCommand(request.Nome, request.Descricao), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/grupos-clientes/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar o grupo.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarGrupoCliente")
            .WithTags("Clientes");

        app.MapGet("/api/v1/grupos-clientes", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarGruposClienteQuery(), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarGruposCliente")
            .WithTags("Clientes");

        // -------- Clientes --------
        app.MapPost("/api/v1/clientes", async (CriarClienteRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new CriarClienteCommand(
                    request.RazaoSocial, request.Cnpj, request.CodigoExterno, request.GrupoClienteId,
                    request.RegimeTributario, request.DataEntrada), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/clientes/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível criar o cliente.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("CriarCliente")
            .WithTags("Clientes");

        app.MapGet("/api/v1/clientes", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarClientesQuery(), ct)))
            .RequireAuthorization("QualquerPerfil")
            .WithName("ListarClientes")
            .WithTags("Clientes");

        app.MapGet("/api/v1/clientes/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ObterClientePorIdQuery(id), ct);
                return resultado.IsSuccess
                    ? Results.Ok(resultado.Value)
                    : Results.Problem(title: "Cliente não encontrado.", detail: resultado.Error, statusCode: 404);
            })
            .RequireAuthorization("QualquerPerfil")
            .WithName("ObterCliente")
            .WithTags("Clientes");

        app.MapPut("/api/v1/clientes/{id:guid}", async (Guid id, AtualizarClienteRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AtualizarClienteCommand(
                    id, request.RazaoSocial, request.CodigoExterno, request.GrupoClienteId, request.RegimeTributario), ct);
                return resultado.IsSuccess
                    ? Results.NoContent()
                    : Results.Problem(title: "Não foi possível atualizar o cliente.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AtualizarCliente")
            .WithTags("Clientes");

        app.MapPost("/api/v1/clientes/{id:guid}/suspender", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new SuspenderClienteCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("SuspenderCliente")
            .WithTags("Clientes");

        app.MapPost("/api/v1/clientes/{id:guid}/inativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new InativarClienteCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("InativarCliente")
            .WithTags("Clientes");

        app.MapPost("/api/v1/clientes/{id:guid}/reativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ReativarClienteCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("ReativarCliente")
            .WithTags("Clientes");

        // -------- Contatos do cliente --------
        app.MapPost("/api/v1/clientes/{id:guid}/contatos", async (Guid id, AdicionarContatoRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AdicionarContatoCommand(id, request.Nome, request.Email, request.Telefone, request.Celular), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/clientes/{id}/contatos/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível adicionar o contato.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AdicionarContatoCliente")
            .WithTags("Clientes");

        app.MapPost("/api/v1/contatos-cliente/{id:guid}/desativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new DesativarContatoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("DesativarContatoCliente")
            .WithTags("Clientes");

        app.MapPost("/api/v1/contatos-cliente/{id:guid}/reativar", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ReativarContatoCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("ReativarContatoCliente")
            .WithTags("Clientes");

        // -------- Responsáveis por cliente (por equipe) --------
        app.MapPost("/api/v1/clientes/{id:guid}/responsaveis", async (Guid id, AdicionarResponsavelRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new AdicionarResponsavelCommand(id, request.EquipeId, request.UsuarioId), ct);
                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/clientes/{id}/responsaveis/{resultado.Value}", new { id = resultado.Value })
                    : Results.Problem(title: "Não foi possível adicionar o responsável.", detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("AdicionarResponsavelCliente")
            .WithTags("Clientes");

        app.MapDelete("/api/v1/responsaveis-cliente/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new RemoverResponsavelCommand(id), ct);
                return resultado.IsSuccess ? Results.NoContent() : Results.Problem(detail: resultado.Error, statusCode: 422);
            })
            .RequireAuthorization("GestorOuAdmin")
            .WithName("RemoverResponsavelCliente")
            .WithTags("Clientes");

        return app;
    }
}

public sealed record CriarGrupoClienteRequest(string Nome, string? Descricao);

public sealed record CriarClienteRequest(
    string RazaoSocial, string Cnpj, string? CodigoExterno, Guid? GrupoClienteId, RegimeTributario RegimeTributario, DateOnly DataEntrada);

public sealed record AtualizarClienteRequest(string RazaoSocial, string? CodigoExterno, Guid? GrupoClienteId, RegimeTributario RegimeTributario);

public sealed record AdicionarContatoRequest(string Nome, string? Email, string? Telefone, string? Celular);

public sealed record AdicionarResponsavelRequest(Guid EquipeId, Guid UsuarioId);
