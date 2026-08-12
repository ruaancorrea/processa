using Microsoft.AspNetCore.Http;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

/// <summary>
/// Resolve o tenant a partir da claim "tenant_id" do JWT autenticado. Sem token (rotas
/// anônimas como login/cadastro) -> Guid.Empty, que não corresponde a tenant nenhum —
/// fail-closed por padrão (ver ADR-002). Repositórios que precisam operar sem tenant
/// (login, unicidade de e-mail) usam IgnoreQueryFilters() explicitamente, nunca dependem
/// deste valor "vazio" para enxergar tudo.
/// </summary>
public sealed class HttpContextTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    public Guid TenantId
    {
        get
        {
            var valor = httpContextAccessor.HttpContext?.User.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(valor, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}
