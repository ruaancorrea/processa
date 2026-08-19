using Microsoft.AspNetCore.Http;
using Processa.Shared.Kernel;
using System.IdentityModel.Tokens.Jwt;

namespace Processa.Modules.Identidade.Infrastructure;

/// <summary>Resolve o usuário autenticado a partir das claims "sub"/"perfil" do JWT — mesmo espírito de HttpContextTenantContext.</summary>
public sealed class HttpContextUsuarioContext(IHttpContextAccessor httpContextAccessor) : IUsuarioContext
{
    public Guid UsuarioId
    {
        get
        {
            var valor = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(valor, out var usuarioId) ? usuarioId : Guid.Empty;
        }
    }

    public Perfil Perfil
    {
        get
        {
            var valor = httpContextAccessor.HttpContext?.User.FindFirst("perfil")?.Value;
            return Enum.TryParse<Perfil>(valor, out var perfil) ? perfil : Perfil.Analista;
        }
    }
}
