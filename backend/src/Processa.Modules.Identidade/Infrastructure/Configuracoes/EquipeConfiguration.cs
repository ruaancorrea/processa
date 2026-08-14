using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Configuracoes;

public sealed class EquipeConfiguration : IEntityTypeConfiguration<Equipe>
{
    public void Configure(EntityTypeBuilder<Equipe> builder)
    {
        builder.ToTable("equipes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.HasIndex(e => e.TenantId);

        builder.Property(e => e.Nome).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Descricao).HasMaxLength(500);
        builder.Property(e => e.Ativa).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.Ignore(e => e.DomainEvents);
    }
}
