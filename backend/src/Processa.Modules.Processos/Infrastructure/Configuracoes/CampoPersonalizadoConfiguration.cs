using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class CampoPersonalizadoConfiguration : IEntityTypeConfiguration<CampoPersonalizado>
{
    public void Configure(EntityTypeBuilder<CampoPersonalizado> builder)
    {
        builder.ToTable("campos_personalizados");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId).IsRequired();
        builder.Property(c => c.TipoProcessoId).IsRequired();
        builder.Property(c => c.Nome).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Tipo).HasConversion<string>().HasMaxLength(20);

        // ValueComparer explícito — sem ele o EF avisa (Model.Validation 10620) que não
        // sabe comparar o conteúdo da lista convertida para detectar mudança real vs.
        // troca de referência (AtualizarDados sempre atribui uma lista NOVA a Opcoes).
        builder.Property(c => c.Opcoes)
            .HasConversion(
                opcoes => JsonSerializer.Serialize(opcoes, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>(),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a ?? Array.Empty<string>()).SequenceEqual(b ?? Array.Empty<string>()),
                    v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                    v => v.ToList()))
            .HasColumnName("opcoes")
            .HasColumnType("jsonb");

        builder.Property(c => c.Obrigatorio).IsRequired();
        builder.Property(c => c.Ordem).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.TipoProcessoId });

        builder.Ignore(c => c.DomainEvents);
    }
}
