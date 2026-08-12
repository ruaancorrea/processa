using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Configuracoes;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();

        builder.OwnsOne(t => t.Cnpj, cnpj =>
        {
            cnpj.Property(c => c.Numero).HasColumnName("cnpj").HasMaxLength(14).IsRequired();
            cnpj.HasIndex(c => c.Numero).IsUnique();
        });

        builder.Property(t => t.DominioPortal).HasMaxLength(255);
        builder.Property(t => t.Plano).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.Ignore(t => t.DomainEvents);
    }
}
