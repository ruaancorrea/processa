using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Infrastructure;

public sealed class ProcessosDbContext(DbContextOptions<ProcessosDbContext> options, ITenantContext tenantContext)
    : DbContext(options), Application.IUnitOfWork
{
    public DbSet<TipoProcesso> TiposProcesso => Set<TipoProcesso>();
    public DbSet<CampoPersonalizado> CamposPersonalizados => Set<CampoPersonalizado>();
    public DbSet<Fluxo> Fluxos => Set<Fluxo>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("processos");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessosDbContext).Assembly);

        modelBuilder.Entity<TipoProcesso>().HasQueryFilter(t => t.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<CampoPersonalizado>().HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Fluxo>().HasQueryFilter(f => f.TenantId == tenantContext.TenantId);
    }

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
