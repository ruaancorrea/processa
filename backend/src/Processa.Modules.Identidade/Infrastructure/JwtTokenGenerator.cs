using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Domain;
using System.IdentityModel.Tokens.Jwt;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class JwtTokenGenerator(IOptions<JwtOptions> options) : IJwtTokenGenerator
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenGerado GerarAccessToken(Usuario usuario)
    {
        var expiraEm = DateTimeOffset.UtcNow.AddMinutes(_options.AccessTokenMinutos);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email.Valor),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("tenant_id", usuario.TenantId.ToString()),
            new("perfil", usuario.Perfil.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Role, usuario.Perfil.ToString()),
        };

        var credenciais = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credenciais);

        return new AccessTokenGerado(new JwtSecurityTokenHandler().WriteToken(token), expiraEm);
    }

    public string GerarRefreshTokenOpaco() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public string HashDoRefreshToken(string tokenOpaco) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(tokenOpaco)));
}
