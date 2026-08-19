using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class ExecucaoEtapaConfiguration : IEntityTypeConfiguration<ExecucaoEtapa>
{
    public void Configure(EntityTypeBuilder<ExecucaoEtapa> builder)
    {
        builder.ToTable("execucao_etapas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.DemandaId).IsRequired();
        builder.Property(e => e.EtapaId).IsRequired();
        builder.Property(e => e.ResponsavelId);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.IniciadoEm).IsRequired();
        builder.Property(e => e.ConcluidoEm);

        // Não é polimórfico (dicionário simples), jsonb é seguro aqui — ao contrário de
        // Etapa.Configuracao (ver EtapaConfiguration), sem discriminador de tipo pra se
        // preocupar com ordem de chave.
        builder.Property(e => e.DadosExecucao)
            .HasConversion(
                dados => JsonSerializer.Serialize(dados, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<Dictionary<string, string>>(json, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>(),
                new ValueComparer<IReadOnlyDictionary<string, string>>(
                    (a, b) => (a ?? new Dictionary<string, string>()).SequenceEqual(b ?? new Dictionary<string, string>()),
                    v => v.Aggregate(0, (hash, kv) => HashCode.Combine(hash, kv.Key.GetHashCode(), kv.Value.GetHashCode())),
                    v => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>(v)))
            .HasColumnName("dados_execucao")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.DemandaId });
        builder.HasIndex(e => new { e.TenantId, e.DemandaId, e.EtapaId }).IsUnique();

        builder.Ignore(e => e.DomainEvents);
    }
}
