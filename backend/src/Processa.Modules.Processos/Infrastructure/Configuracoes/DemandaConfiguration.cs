using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class DemandaConfiguration : IEntityTypeConfiguration<Demanda>
{
    public void Configure(EntityTypeBuilder<Demanda> builder)
    {
        builder.ToTable("demandas");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.TipoProcessoId).IsRequired();
        builder.Property(d => d.FluxoAtivoId).IsRequired();
        builder.Property(d => d.ClienteId).IsRequired();
        builder.Property(d => d.ResponsavelId);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Prioridade).HasConversion<string>().HasMaxLength(10);
        builder.Property(d => d.PercentualConclusao).IsRequired();
        builder.Property(d => d.DataInicio).IsRequired();
        builder.Property(d => d.DataFimPrevista);
        builder.Property(d => d.DataFimReal);
        builder.Property(d => d.EtapaAtualId);
        builder.Property(d => d.DemandaPaiId);
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();

        builder.HasIndex(d => new { d.TenantId, d.Status });
        builder.HasIndex(d => new { d.TenantId, d.ResponsavelId });
        builder.HasIndex(d => new { d.TenantId, d.ClienteId });
        builder.HasIndex(d => new { d.TenantId, d.TipoProcessoId });

        builder.Ignore(d => d.DomainEvents);
    }
}
