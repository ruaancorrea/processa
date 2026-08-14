using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Configuracoes;

public sealed class MembroEquipeConfiguration : IEntityTypeConfiguration<MembroEquipe>
{
    public void Configure(EntityTypeBuilder<MembroEquipe> builder)
    {
        builder.ToTable("membros_equipe");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.TenantId).IsRequired();
        builder.Property(m => m.EquipeId).IsRequired();
        builder.Property(m => m.UsuarioId).IsRequired();
        builder.Property(m => m.Papel).HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.CreatedAt).IsRequired();

        // Um usuário não pode ser adicionado duas vezes na mesma equipe.
        builder.HasIndex(m => new { m.EquipeId, m.UsuarioId }).IsUnique();
        builder.HasIndex(m => m.UsuarioId);

        builder.Ignore(m => m.DomainEvents);
    }
}
