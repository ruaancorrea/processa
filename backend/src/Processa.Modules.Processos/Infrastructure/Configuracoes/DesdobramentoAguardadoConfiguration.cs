using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class DesdobramentoAguardadoConfiguration : IEntityTypeConfiguration<DesdobramentoAguardado>
{
    public void Configure(EntityTypeBuilder<DesdobramentoAguardado> builder)
    {
        builder.ToTable("desdobramentos_aguardados");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.TenantId).IsRequired();
        builder.Property(d => d.ExecucaoEtapaUniaoId).IsRequired();
        builder.Property(d => d.ExecucaoEtapaCondicionalId).IsRequired();
        builder.Property(d => d.Concluido).IsRequired();

        builder.HasIndex(d => d.ExecucaoEtapaUniaoId);
        builder.HasIndex(d => d.ExecucaoEtapaCondicionalId).IsUnique();

        builder.Ignore(d => d.DomainEvents);
    }
}
