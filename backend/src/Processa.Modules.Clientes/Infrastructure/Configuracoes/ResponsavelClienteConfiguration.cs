using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Configuracoes;

public sealed class ResponsavelClienteConfiguration : IEntityTypeConfiguration<ResponsavelCliente>
{
    public void Configure(EntityTypeBuilder<ResponsavelCliente> builder)
    {
        builder.ToTable("responsaveis_cliente");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.TenantId).IsRequired();
        builder.Property(r => r.ClienteId).IsRequired();
        builder.Property(r => r.EquipeId).IsRequired();
        builder.Property(r => r.UsuarioId).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.RemovidoEm);

        // Índice único PARCIAL — permite reativar o mesmo vínculo depois de um soft-delete
        // sem violar unicidade (ver docs/03-modelagem/modelo-de-dados.md).
        builder.HasIndex(r => new { r.TenantId, r.ClienteId, r.UsuarioId, r.EquipeId })
            .IsUnique()
            .HasFilter("\"RemovidoEm\" IS NULL");

        builder.HasIndex(r => r.ClienteId);

        builder.Ignore(r => r.DomainEvents);
    }
}
