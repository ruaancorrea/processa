using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class ComentarioExecucaoConfiguration : IEntityTypeConfiguration<ComentarioExecucao>
{
    public void Configure(EntityTypeBuilder<ComentarioExecucao> builder)
    {
        builder.ToTable("comentarios_execucao");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.ExecucaoEtapaId).IsRequired();
        builder.Property(c => c.UsuarioId).IsRequired();
        builder.Property(c => c.Texto).HasMaxLength(4000).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();

        builder.HasIndex(c => c.ExecucaoEtapaId);

        builder.Ignore(c => c.DomainEvents);
    }
}
