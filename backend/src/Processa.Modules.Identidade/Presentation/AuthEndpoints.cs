using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Identidade.Application.Auth;
using Processa.Modules.Identidade.Application.Tenants;

namespace Processa.Modules.Identidade.Presentation;

/// <summary>
/// Refresh token viaja como cookie httpOnly+Secure+SameSite=Strict — nunca acessível a
/// JavaScript (mitiga roubo via XSS). Access token vai no corpo da resposta; o frontend
/// mantém em memória, nunca em localStorage. Ver .faf/decisions.faf.
/// </summary>
public static class AuthEndpoints
{
    private const string RefreshCookieName = "processa_refresh";

    public static IEndpointRouteBuilder MapIdentidadeModule(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/tenants", async (CriarTenantRequest request, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(
                    new CriarTenantCommand(request.NomeEscritorio, request.Cnpj, request.NomeAdmin, request.EmailAdmin, request.Senha),
                    ct);

                return resultado.IsSuccess
                    ? Results.Created($"/api/v1/tenants/{resultado.Value.TenantId}", resultado.Value)
                    : Results.Problem(title: "Não foi possível criar o escritório.", detail: resultado.Error, statusCode: 422);
            })
            .WithName("CriarTenant")
            .WithTags("Identidade");

        app.MapPost("/api/v1/auth/login", async (LoginRequest request, ISender sender, HttpResponse response, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new LoginCommand(request.Email, request.Senha), ct);

                if (resultado.IsFailure)
                    return Results.Problem(title: "Falha na autenticação.", detail: resultado.Error, statusCode: 401);

                DefinirCookieDeRefresh(response, resultado.Value.RefreshTokenOpaco);
                return Results.Ok(ParaRespostaPublica(resultado.Value));
            })
            .WithName("Login")
            .WithTags("Identidade");

        app.MapPost("/api/v1/auth/refresh", async (HttpRequest request, HttpResponse response, ISender sender, CancellationToken ct) =>
            {
                if (!request.Cookies.TryGetValue(RefreshCookieName, out var refreshTokenOpaco) || string.IsNullOrEmpty(refreshTokenOpaco))
                    return Results.Problem(title: "Sessão expirada.", detail: "Faça login novamente.", statusCode: 401);

                var resultado = await sender.Send(new RefreshTokenCommand(refreshTokenOpaco), ct);

                if (resultado.IsFailure)
                {
                    response.Cookies.Delete(RefreshCookieName);
                    return Results.Problem(title: "Sessão expirada.", detail: resultado.Error, statusCode: 401);
                }

                DefinirCookieDeRefresh(response, resultado.Value.RefreshTokenOpaco);
                return Results.Ok(ParaRespostaPublica(resultado.Value));
            })
            .WithName("RefreshToken")
            .WithTags("Identidade");

        app.MapPost("/api/v1/auth/logout", async (HttpRequest request, HttpResponse response, ISender sender, CancellationToken ct) =>
            {
                if (request.Cookies.TryGetValue(RefreshCookieName, out var refreshTokenOpaco) && !string.IsNullOrEmpty(refreshTokenOpaco))
                    await sender.Send(new LogoutCommand(refreshTokenOpaco), ct);

                response.Cookies.Delete(RefreshCookieName);
                return Results.NoContent();
            })
            .WithName("Logout")
            .WithTags("Identidade");

        // Endpoints de exemplo para validar RBAC de ponta a ponta (PROJ-35) — todo
        // endpoint de negócio real (Sprint 2+) segue este mesmo padrão de policy.
        app.MapGet("/api/v1/me", (ClaimsPrincipal user) => Results.Ok(new
            {
                UsuarioId = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value,
                TenantId = user.FindFirst("tenant_id")?.Value,
                Perfil = user.FindFirst("perfil")?.Value,
                Nome = user.Identity?.Name,
            }))
            .RequireAuthorization("QualquerPerfil")
            .WithName("MeuUsuario")
            .WithTags("Identidade");

        app.MapGet("/api/v1/admin/ping", () => Results.Ok(new { status = "ok" }))
            .RequireAuthorization("Admin")
            .WithName("AdminPing")
            .WithTags("Identidade");

        return app;
    }

    private static void DefinirCookieDeRefresh(HttpResponse response, string refreshTokenOpaco)
    {
        response.Cookies.Append(RefreshCookieName, refreshTokenOpaco, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(7),
        });
    }

    private static LoginResponse ParaRespostaPublica(LoginResultado resultado) => new(
        resultado.AccessToken,
        resultado.AccessTokenExpiraEm,
        resultado.UsuarioId,
        resultado.TenantId,
        resultado.Perfil.ToString());
}

public sealed record CriarTenantRequest(string NomeEscritorio, string Cnpj, string NomeAdmin, string EmailAdmin, string Senha);

public sealed record LoginRequest(string Email, string Senha);

public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiraEm, Guid UsuarioId, Guid TenantId, string Perfil);
