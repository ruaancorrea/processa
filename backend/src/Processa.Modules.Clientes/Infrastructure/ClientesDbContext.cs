using Microsoft.EntityFrameworkCore;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure;

public sealed class ClientesDbContext(DbContextOptions<ClientesDbContext> options, ITenantContext tenantContext)
    : DbContext(options), Application.IUnitOfWork
{
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<GrupoCliente> GruposCliente => Set<GrupoCliente>();
    public DbSet<ContatoCliente> ContatosCliente => Set<ContatoCliente>();
    public DbSet<ResponsavelCliente> ResponsaveisCliente => Set<ResponsavelCliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("clientes");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClientesDbContext).Assembly);

        modelBuilder.Entity<Cliente>().HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<GrupoCliente>().HasQueryFilter(g => g.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<ContatoCliente>().HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<ResponsavelCliente>().HasQueryFilter(r => r.TenantId == tenantContext.TenantId);
    }

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
