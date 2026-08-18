using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class TipoProcessoConfiguration : IEntityTypeConfiguration<TipoProcesso>
{
    public void Configure(EntityTypeBuilder<TipoProcesso> builder)
    {
        builder.ToTable("tipos_processo");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.EquipeId).IsRequired();
        builder.Property(t => t.Nome).HasMaxLength(200).IsRequired();
        builder.Property(t => t.Descricao).HasMaxLength(1000);
        builder.Property(t => t.ResponsavelObrigatorio).IsRequired();
        builder.Property(t => t.ModoAtribuicao).HasConversion<string>().HasMaxLength(20);
        builder.Property(t => t.ResponsavelFixoId);

        // Serializado como JSON — PermissoesInicio é um VO sem construtor público
        // nem setters (EF não consegue materializá-lo via OwnsOne().ToJson()), e o
        // conteúdo (perfis + usuário-ids) não participa de índice ou filtro no banco.
        builder.Property(t => t.PermissoesInicio)
            .HasConversion(
                permissoes => JsonSerializer.Serialize(
                    new PermissoesInicioJson(permissoes.Perfis.ToList(), permissoes.UsuarioIds.ToList()), (JsonSerializerOptions?)null),
                json => Desserializar(json))
            .HasColumnName("permissoes_inicio")
            .HasColumnType("jsonb");

        builder.Property(t => t.Ativo).IsRequired();
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.EquipeId });

        builder.Ignore(t => t.DomainEvents);
    }

    private static PermissoesInicio Desserializar(string json)
    {
        var dto = JsonSerializer.Deserialize<PermissoesInicioJson>(json, (JsonSerializerOptions?)null);
        return PermissoesInicio.Criar(dto?.Perfis, dto?.UsuarioIds);
    }

    private sealed record PermissoesInicioJson(List<Perfil> Perfis, List<Guid> UsuarioIds);
}
