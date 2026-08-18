using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Infrastructure.Configuracoes;

public sealed class EtapaConfiguration : IEntityTypeConfiguration<Etapa>
{
    public void Configure(EntityTypeBuilder<Etapa> builder)
    {
        builder.ToTable("etapas");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.TenantId).IsRequired();
        builder.Property(e => e.FluxoId).IsRequired();
        builder.Property(e => e.Nome).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Descricao).HasMaxLength(1000);
        builder.Property(e => e.Tipo).HasConversion<string>().HasMaxLength(20);
        builder.Property(e => e.Ordem).IsRequired();

        // Configuracao é polimórfica (ConfiguracaoEtapa abstrata + 6 tipos derivados,
        // ver Domain/ConfiguracaoEtapa.cs) — serializada via System.Text.Json com os
        // atributos [JsonPolymorphic]/[JsonDerivedType] já declarados no tipo base,
        // não OwnsOne().ToJson() (mesmo motivo do PermissoesInicio no Sprint 3).
        // Coluna "json", NÃO "jsonb" — achado real testando manualmente: jsonb no
        // Postgres reordena as chaves do objeto ao armazenar (não preserva a ordem
        // original), e a leitura polimórfica do System.Text.Json EXIGE que o
        // discriminador "tipo" seja a PRIMEIRA propriedade do objeto, senão lança
        // NotSupportedException mesmo com o discriminador presente. "json" armazena o
        // texto original literal (sem reordenar); como este campo nunca é consultado
        // via JSON path no SQL, perder a indexação binária do jsonb aqui não custa nada.
        builder.Property(e => e.Configuracao)
            .HasConversion(c => SerializarConfiguracao(c), j => DesserializarConfiguracao(j))
            .HasColumnName("configuracao")
            .HasColumnType("json");

        builder.Property(e => e.ConfiguracaoAcesso)
            .HasConversion(
                configuracaoAcesso => JsonSerializer.Serialize(
                    new ConfiguracaoAcessoEtapaJson(
                        configuracaoAcesso.PodeAlterar.ToList(), configuracaoAcesso.UsuarioIdsPodeAlterar.ToList()),
                    (JsonSerializerOptions?)null),
                json => Desserializar(json))
            .HasColumnName("configuracao_acesso")
            .HasColumnType("jsonb");

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.FluxoId });

        builder.Ignore(e => e.DomainEvents);
    }

    private static ConfiguracaoAcessoEtapa Desserializar(string json)
    {
        var dto = JsonSerializer.Deserialize<ConfiguracaoAcessoEtapaJson>(json, (JsonSerializerOptions?)null);
        return ConfiguracaoAcessoEtapa.Criar(dto?.PodeAlterar, dto?.UsuarioIdsPodeAlterar);
    }

    private static string? SerializarConfiguracao(ConfiguracaoEtapa? configuracao) =>
        configuracao is null ? null : JsonSerializer.Serialize(configuracao, (JsonSerializerOptions?)null);

    private static ConfiguracaoEtapa? DesserializarConfiguracao(string? json) =>
        string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<ConfiguracaoEtapa>(json, (JsonSerializerOptions?)null);

    private sealed record ConfiguracaoAcessoEtapaJson(List<Perfil> PodeAlterar, List<Guid> UsuarioIdsPodeAlterar);
}
