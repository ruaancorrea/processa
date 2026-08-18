using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Um passo dentro de um Fluxo. O "comportamento" da etapa (Tipo) e sua Configuracao
/// definem COMO ela é decidida em tempo de execução real — essa decisão em si (o
/// handler, ver IEtapaHandler) só roda contra uma Demanda de verdade a partir do
/// Sprint 5; aqui a etapa é configuração pura, igual TipoProcesso/Fluxo no Sprint 3.
/// </summary>
public sealed class Etapa : Entity
{
    public Guid TenantId { get; private set; }
    public Guid FluxoId { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public TipoEtapa Tipo { get; private set; }
    public int Ordem { get; private set; }
    public ConfiguracaoEtapa? Configuracao { get; private set; }
    public ConfiguracaoAcessoEtapa ConfiguracaoAcesso { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Etapa()
    {
        Nome = string.Empty;
        ConfiguracaoAcesso = ConfiguracaoAcessoEtapa.Vazia;
    }

    private Etapa(Guid id, Guid tenantId, Guid fluxoId, string nome, string? descricao, TipoEtapa tipo, int ordem, ConfiguracaoEtapa? configuracao)
        : base(id)
    {
        TenantId = tenantId;
        FluxoId = fluxoId;
        Nome = nome;
        Descricao = descricao;
        Tipo = tipo;
        Ordem = ordem;
        Configuracao = configuracao;
        ConfiguracaoAcesso = ConfiguracaoAcessoEtapa.Vazia;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Etapa> Criar(
        Guid tenantId, Guid fluxoId, string? nome, string? descricao, TipoEtapa tipo, int ordem, ConfiguracaoEtapa? configuracao)
    {
        if (tenantId == Guid.Empty || fluxoId == Guid.Empty)
            return Result.Failure<Etapa>("Tenant e fluxo são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Etapa>("O nome da etapa é obrigatório.");

        var validacaoConfiguracao = ValidarConfiguracaoParaTipo(tipo, configuracao);
        if (validacaoConfiguracao.IsFailure)
            return Result.Failure<Etapa>(validacaoConfiguracao.Error!);

        return Result.Success(new Etapa(Guid.NewGuid(), tenantId, fluxoId, nome.Trim(), descricao?.Trim(), tipo, ordem, configuracao));
    }

    public Result AtualizarDados(string? nome, string? descricao, int ordem, ConfiguracaoEtapa? configuracao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("O nome da etapa é obrigatório.");

        var validacaoConfiguracao = ValidarConfiguracaoParaTipo(Tipo, configuracao);
        if (validacaoConfiguracao.IsFailure)
            return validacaoConfiguracao;

        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        Ordem = ordem;
        Configuracao = configuracao;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void DefinirConfiguracaoAcesso(ConfiguracaoAcessoEtapa configuracaoAcesso)
    {
        ConfiguracaoAcesso = configuracaoAcesso;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Comum e Conclusão não têm nada a configurar (a primeira só espera ação manual,
    /// a segunda só marca o fim do fluxo) — os outros 6 tipos exigem sua Configuracao
    /// correspondente, validada pelo próprio record (Configuracao.Validar()).
    /// </summary>
    private static Result ValidarConfiguracaoParaTipo(TipoEtapa tipo, ConfiguracaoEtapa? configuracao)
    {
        var exigeConfiguracao = tipo is not (TipoEtapa.Comum or TipoEtapa.Conclusao);

        if (!exigeConfiguracao)
            return configuracao is null
                ? Result.Success()
                : Result.Failure($"Etapa do tipo {tipo} não aceita configuração.");

        if (configuracao is null)
            return Result.Failure($"Etapa do tipo {tipo} exige configuração.");

        var tipoCasaComConfiguracao = tipo switch
        {
            TipoEtapa.Condicional => configuracao is ConfiguracaoEtapaCondicional,
            TipoEtapa.Automatizada => configuracao is ConfiguracaoEtapaAutomatizada,
            TipoEtapa.Notificacao => configuracao is ConfiguracaoEtapaNotificacao,
            TipoEtapa.Agendamento => configuracao is ConfiguracaoEtapaAgendamento,
            TipoEtapa.Subprocesso => configuracao is ConfiguracaoEtapaSubprocesso,
            TipoEtapa.Uniao => configuracao is ConfiguracaoEtapaUniao,
            _ => false,
        };

        if (!tipoCasaComConfiguracao)
            return Result.Failure($"Configuração incompatível com o tipo de etapa {tipo}.");

        return configuracao.Validar();
    }
}
