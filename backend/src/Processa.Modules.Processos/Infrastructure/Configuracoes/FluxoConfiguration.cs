using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class FluxoConfiguration : IEntityTypeConfiguration<Fluxo>
{
    public void Configure(EntityTypeBuilder<Fluxo> builder)
    {
        builder.ToTable("fluxos");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.TenantId).IsRequired();
        builder.Property(f => f.TipoProcessoId).IsRequired();
        builder.Property(f => f.Nome).HasMaxLength(200).IsRequired();
        builder.Property(f => f.Descricao).HasMaxLength(1000);
        builder.Property(f => f.FluxoPadrao).IsRequired().HasDefaultValue(false);
        builder.Property(f => f.CreatedAt).IsRequired();
        builder.Property(f => f.UpdatedAt).IsRequired();

        // Índice único parcial — garante um único fluxo padrão por tipo de processo
        // (docs/03-modelagem/modelo-de-dados.md). É o backstop real da invariante;
        // a Application layer (CriarFluxoCommand/DefinirFluxoPadraoCommand) já
        // desmarca o padrão anterior antes de marcar o novo na mesma transação.
        builder.HasIndex(f => f.TipoProcessoId)
            .IsUnique()
            .HasFilter("\"FluxoPadrao\" = true");

        // Índice PARCIAL acima só cobre linhas com FluxoPadrao=true — não ajuda
        // ListarPorTipoProcessoAsync/ObterPadraoAsync, que leem TODAS as linhas de um
        // tipo de processo. Mesmo índice de suporte já usado em TipoProcessoConfiguration
        // e CampoPersonalizadoConfiguration.
        builder.HasIndex(f => new { f.TenantId, f.TipoProcessoId });

        builder.Ignore(f => f.DomainEvents);
    }
}
