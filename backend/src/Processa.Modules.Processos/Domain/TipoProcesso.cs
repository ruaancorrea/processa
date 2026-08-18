using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Template que define como uma categoria de trabalho deve ser executada — não é
/// uma execução em si (isso é Demanda, Sprint 5). EquipeId e ResponsavelFixoId são
/// Guid puro: Processos não pode referenciar o Domain de Identidade diretamente
/// (ver .claude/architecture.md) — validados via IVerificadorEquipe/
/// IVerificadorMembroEquipe na Application layer, não aqui.
/// </summary>
public sealed class TipoProcesso : Entity
{
    public Guid TenantId { get; private set; }
    public Guid EquipeId { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public bool ResponsavelObrigatorio { get; private set; }
    public ModoAtribuicao ModoAtribuicao { get; private set; }
    public Guid? ResponsavelFixoId { get; private set; }
    public PermissoesInicio PermissoesInicio { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private TipoProcesso()
    {
        Nome = string.Empty;
        PermissoesInicio = PermissoesInicio.Vazia;
    }

    private TipoProcesso(
        Guid id, Guid tenantId, Guid equipeId, string nome, string? descricao,
        bool responsavelObrigatorio, ModoAtribuicao modoAtribuicao, Guid? responsavelFixoId) : base(id)
    {
        TenantId = tenantId;
        EquipeId = equipeId;
        Nome = nome;
        Descricao = descricao;
        ResponsavelObrigatorio = responsavelObrigatorio;
        ModoAtribuicao = modoAtribuicao;
        ResponsavelFixoId = responsavelFixoId;
        PermissoesInicio = PermissoesInicio.Vazia;
        Ativo = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<TipoProcesso> Criar(
        Guid tenantId, Guid equipeId, string? nome, string? descricao,
        bool responsavelObrigatorio, ModoAtribuicao modoAtribuicao, Guid? responsavelFixoId)
    {
        if (tenantId == Guid.Empty || equipeId == Guid.Empty)
            return Result.Failure<TipoProcesso>("Tenant e equipe são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<TipoProcesso>("O nome do tipo de processo é obrigatório.");

        var validacaoModo = ValidarModoAtribuicao(modoAtribuicao, responsavelFixoId);
        if (validacaoModo.IsFailure)
            return Result.Failure<TipoProcesso>(validacaoModo.Error!);

        return Result.Success(new TipoProcesso(
            Guid.NewGuid(), tenantId, equipeId, nome.Trim(), descricao?.Trim(),
            responsavelObrigatorio, modoAtribuicao, modoAtribuicao == ModoAtribuicao.Fixo ? responsavelFixoId : null));
    }

    public Result AtualizarDados(string? nome, string? descricao, bool responsavelObrigatorio, ModoAtribuicao modoAtribuicao, Guid? responsavelFixoId)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("O nome do tipo de processo é obrigatório.");

        var validacaoModo = ValidarModoAtribuicao(modoAtribuicao, responsavelFixoId);
        if (validacaoModo.IsFailure)
            return validacaoModo;

        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        ResponsavelObrigatorio = responsavelObrigatorio;
        ModoAtribuicao = modoAtribuicao;
        ResponsavelFixoId = modoAtribuicao == ModoAtribuicao.Fixo ? responsavelFixoId : null;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void DefinirPermissoesInicio(PermissoesInicio permissoes)
    {
        PermissoesInicio = permissoes;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Ativar()
    {
        Ativo = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Desativar()
    {
        Ativo = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static Result ValidarModoAtribuicao(ModoAtribuicao modoAtribuicao, Guid? responsavelFixoId)
    {
        if (modoAtribuicao == ModoAtribuicao.Fixo && (responsavelFixoId is null || responsavelFixoId == Guid.Empty))
            return Result.Failure("Modo de atribuição fixo exige um responsável fixo.");

        return Result.Success();
    }
}
