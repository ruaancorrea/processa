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
/// JavaScript no browser do cliente (mitiga roubo via XSS), path restrito a /api/v1/auth.
/// Access token vai no corpo da resposta; cabe ao cliente decidir onde guardar (a
/// recomendação é memória, nunca localStorage). Ver .faf/decisions.faf.
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
            .WithTags("Identidade")
            .WithSummary("Cadastra um novo escritório (onboarding)")
            .WithDescription("Cria o tenant e seu primeiro usuário Admin numa única operação. Rota pública — não exige autenticação prévia.");

        app.MapPost("/api/v1/auth/login", async (LoginRequest request, ISender sender, HttpResponse response, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new LoginCommand(request.Email, request.Senha), ct);

                if (resultado.IsFailure)
                    return Results.Problem(title: "Falha na autenticação.", detail: resultado.Error, statusCode: 401);

                DefinirCookieDeRefresh(response, resultado.Value.RefreshTokenOpaco);
                return Results.Ok(ParaRespostaPublica(resultado.Value));
            })
            .WithName("Login")
            .WithTags("Identidade")
            .WithSummary("Login")
            .WithDescription("Retorna o access token no corpo e define o refresh token num cookie httpOnly. Cole o access token no botão \"Authorize\" desta página pra testar as rotas protegidas.");

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
            .WithTags("Identidade")
            .WithSummary("Renova o access token")
            .WithDescription("Usa o refresh token do cookie httpOnly (não vai no corpo/query — nunca visível a JS). Rotativo: emite um refresh token novo a cada uso e invalida o anterior.");

        app.MapPost("/api/v1/auth/logout", async (HttpRequest request, HttpResponse response, ISender sender, CancellationToken ct) =>
            {
                if (request.Cookies.TryGetValue(RefreshCookieName, out var refreshTokenOpaco) && !string.IsNullOrEmpty(refreshTokenOpaco))
                    await sender.Send(new LogoutCommand(refreshTokenOpaco), ct);

                response.Cookies.Delete(RefreshCookieName);
                return Results.NoContent();
            })
            .WithName("Logout")
            .WithTags("Identidade")
            .WithSummary("Revoga o refresh token atual e limpa o cookie");

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
            .WithTags("Identidade")
            .WithSummary("Identidade do usuário autenticado")
            .WithDescription("Lê as claims do próprio JWT — não bate no banco. Útil também como \"rota canário\" pra confirmar que um token colado no Authorize realmente autentica.");

        app.MapGet("/api/v1/admin/ping", () => Results.Ok(new { status = "ok" }))
            .RequireAuthorization("Admin")
            .WithName("AdminPing")
            .WithTags("Identidade")
            .WithSummary("Rota canário de RBAC")
            .WithDescription("Só responde 200 pra perfil Admin — existe pra validar a política de autorização de ponta a ponta em teste de integração, não é uma rota de negócio.");

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

/// <summary>Onboarding de um novo escritório — cria o tenant e seu usuário Admin inicial.</summary>
public sealed record CriarTenantRequest(string NomeEscritorio, string Cnpj, string NomeAdmin, string EmailAdmin, string Senha);

/// <summary>Credenciais de login.</summary>
public sealed record LoginRequest(string Email, string Senha);

/// <summary>Resposta pública de autenticação — o refresh token não aparece aqui, só no cookie httpOnly.</summary>
public sealed record LoginResponse(string AccessToken, DateTimeOffset AccessTokenExpiraEm, Guid UsuarioId, Guid TenantId, string Perfil);
