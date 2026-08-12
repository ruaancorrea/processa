using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

/// <summary>
/// DbContext do módulo Identidade — cada módulo tem o seu, mesmo compartilhando a
/// mesma instância física de Postgres. Ver docs/02-arquitetura/decisoes/adr-001.
/// </summary>
public sealed class IdentidadeDbContext(DbContextOptions<IdentidadeDbContext> options, ITenantContext tenantContext)
    : DbContext(options), Application.IUnitOfWork
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identidade");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IdentidadeDbContext).Assembly);

        // Global Query Filter por tenant (ADR-002) — Usuario e RefreshToken (via Usuario)
        // só enxergam dados do tenant resolvido no contexto da requisição. Tenant em si
        // não é filtrado (é a própria unidade de isolamento). Consultas que legitimamente
        // precisam atravessar tenants (login, checagem de e-mail único) usam
        // IgnoreQueryFilters() explicitamente no repositório, nunca implicitamente aqui.
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.TenantId == tenantContext.TenantId);
    }

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
