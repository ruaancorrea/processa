using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class AnexoExecucaoConfiguration : IEntityTypeConfiguration<AnexoExecucao>
{
    public void Configure(EntityTypeBuilder<AnexoExecucao> builder)
    {
        builder.ToTable("anexos_execucao");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.ExecucaoEtapaId).IsRequired();
        builder.Property(a => a.UsuarioId).IsRequired();
        builder.Property(a => a.NomeOriginal).HasMaxLength(255).IsRequired();
        builder.Property(a => a.NomeArmazenado).HasMaxLength(255).IsRequired();
        builder.Property(a => a.CaminhoStorage).HasMaxLength(500).IsRequired();
        builder.Property(a => a.TamanhoBytes).IsRequired();
        builder.Property(a => a.MimeType).HasMaxLength(150).IsRequired();
        builder.Property(a => a.CreatedAt).IsRequired();

        builder.HasIndex(a => a.ExecucaoEtapaId);

        builder.Ignore(a => a.DomainEvents);
    }
}
