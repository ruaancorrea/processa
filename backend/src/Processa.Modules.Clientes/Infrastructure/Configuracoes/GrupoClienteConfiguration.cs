using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Configuracoes;

public sealed class GrupoClienteConfiguration : IEntityTypeConfiguration<GrupoCliente>
{
    public void Configure(EntityTypeBuilder<GrupoCliente> builder)
    {
        builder.ToTable("grupos_cliente");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.TenantId).IsRequired();
        builder.HasIndex(g => g.TenantId);

        builder.Property(g => g.Nome).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Descricao).HasMaxLength(500);
        builder.Property(g => g.CreatedAt).IsRequired();
        builder.Property(g => g.UpdatedAt).IsRequired();

        builder.Ignore(g => g.DomainEvents);
    }
}
