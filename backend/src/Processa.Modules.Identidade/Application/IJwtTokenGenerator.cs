using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application;

public sealed record AccessTokenGerado(string Token, DateTimeOffset ExpiraEm);

public interface IJwtTokenGenerator
{
    /// <summary>Access token JWT, 15 min, claims: sub, tenant_id, perfil, name, email.</summary>
    AccessTokenGerado GerarAccessToken(Usuario usuario);

    /// <summary>Refresh token opaco (não-JWT) em texto puro — o chamador decide como transportar/armazenar.</summary>
    string GerarRefreshTokenOpaco();

    string HashDoRefreshToken(string tokenOpaco);
}
