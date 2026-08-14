using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure.Configuracoes;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("clientes");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();

        builder.Property(c => c.RazaoSocial).HasMaxLength(200).IsRequired();

        // HasConversion (não OwnsOne) — Cnpj é um VO de uma coluna só, e o índice
        // composto abaixo (tenant_id, cnpj) precisa de uma propriedade escalar
        // "normal" pro EF Core indexar; navegação de owned type não compõe direto
        // num HasIndex lambda-based (achado real gerando a migration).
        builder.Property(c => c.Cnpj)
            .HasConversion(cnpj => cnpj.Numero, numero => Cnpj.Criar(numero).Value)
            .HasColumnName("cnpj")
            .HasMaxLength(14)
            .IsRequired();

        // Único POR TENANT (não globalmente, ao contrário do e-mail de Usuario) —
        // dois tenants diferentes podem legitimamente atender o mesmo CNPJ.
        builder.HasIndex(c => new { c.TenantId, c.Cnpj }).IsUnique();

        builder.Property(c => c.CodigoExterno).HasMaxLength(100);
        builder.Property(c => c.GrupoClienteId);
        builder.Property(c => c.RegimeTributario).HasConversion<string>().HasMaxLength(30);
        builder.Property(c => c.DataEntrada).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.Ignore(c => c.DomainEvents);
    }
}
