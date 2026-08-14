using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Configuracoes;

public sealed class ContatoClienteConfiguration : IEntityTypeConfiguration<ContatoCliente>
{
    public void Configure(EntityTypeBuilder<ContatoCliente> builder)
    {
        builder.ToTable("contatos_cliente");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.ClienteId).IsRequired();
        builder.HasIndex(c => c.ClienteId);

        builder.Property(c => c.Nome).HasMaxLength(200).IsRequired();

        builder.OwnsOne(c => c.Email, email =>
        {
            email.Property(e => e.Valor).HasColumnName("email").HasMaxLength(255);
        });

        builder.Property(c => c.Telefone).HasMaxLength(20);
        builder.Property(c => c.Celular).HasMaxLength(20);
        builder.Property(c => c.Ativo).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
