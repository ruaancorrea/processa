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
    public DbSet<Etapa> Etapas => Set<Etapa>();
    public DbSet<Demanda> Demandas => Set<Demanda>();
    public DbSet<ExecucaoEtapa> ExecucoesEtapa => Set<ExecucaoEtapa>();
    public DbSet<DesdobramentoAguardado> DesdobramentosAguardados => Set<DesdobramentoAguardado>();
    public DbSet<HistoricoExecucaoEtapa> HistoricosExecucaoEtapa => Set<HistoricoExecucaoEtapa>();
    public DbSet<ComentarioExecucao> ComentariosExecucao => Set<ComentarioExecucao>();
    public DbSet<AnexoExecucao> AnexosExecucao => Set<AnexoExecucao>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("processos");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProcessosDbContext).Assembly);

        modelBuilder.Entity<TipoProcesso>().HasQueryFilter(t => t.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<CampoPersonalizado>().HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Fluxo>().HasQueryFilter(f => f.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Etapa>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<Demanda>().HasQueryFilter(d => d.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<ExecucaoEtapa>().HasQueryFilter(e => e.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<DesdobramentoAguardado>().HasQueryFilter(d => d.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<HistoricoExecucaoEtapa>().HasQueryFilter(h => h.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<ComentarioExecucao>().HasQueryFilter(c => c.TenantId == tenantContext.TenantId);
        modelBuilder.Entity<AnexoExecucao>().HasQueryFilter(a => a.TenantId == tenantContext.TenantId);
    }

    public Task SalvarAsync(CancellationToken ct = default) => SaveChangesAsync(ct);
}
