using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Configuracoes;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.TenantId).IsRequired();
        builder.HasIndex(u => u.TenantId);

        builder.Property(u => u.Nome).HasMaxLength(200).IsRequired();

        builder.OwnsOne(u => u.Email, email =>
        {
            // Único GLOBALMENTE (não escoped por tenant) — ver .faf/decisions.faf.
            email.Property(e => e.Valor).HasColumnName("email").HasMaxLength(255).IsRequired();
            email.HasIndex(e => e.Valor).IsUnique();
        });

        builder.Property(u => u.SenhaHash).HasMaxLength(255).IsRequired();
        builder.Property(u => u.Perfil).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.NivelExperiencia).IsRequired();
        builder.Property(u => u.Ativo).IsRequired();
        builder.Property(u => u.TentativasLoginFalhas).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        builder.Ignore(u => u.DomainEvents);
    }
}
