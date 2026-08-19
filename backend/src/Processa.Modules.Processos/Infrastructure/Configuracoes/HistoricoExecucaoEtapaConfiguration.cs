using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class HistoricoExecucaoEtapaConfiguration : IEntityTypeConfiguration<HistoricoExecucaoEtapa>
{
    public void Configure(EntityTypeBuilder<HistoricoExecucaoEtapa> builder)
    {
        builder.ToTable("historico_execucao_etapa");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.TenantId).IsRequired();
        builder.Property(h => h.ExecucaoEtapaId).IsRequired();
        builder.Property(h => h.TipoEvento).HasConversion<string>().HasMaxLength(30);
        builder.Property(h => h.Dados).HasColumnType("jsonb").IsRequired();
        builder.Property(h => h.UsuarioId);
        builder.Property(h => h.CreatedAt).IsRequired();

        builder.HasIndex(h => h.ExecucaoEtapaId);

        builder.Ignore(h => h.DomainEvents);
    }
}
