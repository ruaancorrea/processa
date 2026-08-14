using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure.Repositorios;

public sealed class TenantRepository(IdentidadeDbContext db) : ITenantRepository
{
    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Cnpj.Numero == cnpj.Numero, ct);

    public async Task AddAsync(Tenant tenant, CancellationToken ct = default) =>
        await db.Tenants.AddAsync(tenant, ct);

    public Task<Tenant?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
}
