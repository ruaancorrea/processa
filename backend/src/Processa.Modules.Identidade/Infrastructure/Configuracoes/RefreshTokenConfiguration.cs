using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Configuracoes;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UsuarioId).IsRequired();
        builder.HasIndex(r => r.UsuarioId);

        builder.Property(r => r.TokenHash).HasMaxLength(255).IsRequired();
        builder.HasIndex(r => r.TokenHash).IsUnique();

        builder.Property(r => r.ExpiraEm).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.Ignore(r => r.DomainEvents);
    }
}
