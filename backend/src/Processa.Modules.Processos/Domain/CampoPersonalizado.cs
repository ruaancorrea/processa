using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>Campo extra do tipo de processo — só o tipo Lista carrega opções.</summary>
public sealed class CampoPersonalizado : Entity
{
    public Guid TenantId { get; private set; }
    public Guid TipoProcessoId { get; private set; }
    public string Nome { get; private set; }
    public TipoCampoPersonalizado Tipo { get; private set; }
    public IReadOnlyList<string> Opcoes { get; private set; }
    public bool Obrigatorio { get; private set; }
    public int Ordem { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private CampoPersonalizado()
    {
        Nome = string.Empty;
        Opcoes = [];
    }

    private CampoPersonalizado(
        Guid id, Guid tenantId, Guid tipoProcessoId, string nome, TipoCampoPersonalizado tipo,
        IReadOnlyList<string> opcoes, bool obrigatorio, int ordem) : base(id)
    {
        TenantId = tenantId;
        TipoProcessoId = tipoProcessoId;
        Nome = nome;
        Tipo = tipo;
        Opcoes = opcoes;
        Obrigatorio = obrigatorio;
        Ordem = ordem;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<CampoPersonalizado> Criar(
        Guid tenantId, Guid tipoProcessoId, string? nome, TipoCampoPersonalizado tipo,
        IEnumerable<string>? opcoes, bool obrigatorio, int ordem)
    {
        if (tenantId == Guid.Empty || tipoProcessoId == Guid.Empty)
            return Result.Failure<CampoPersonalizado>("Tenant e tipo de processo são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<CampoPersonalizado>("O nome do campo é obrigatório.");

        var opcoesLista = (opcoes ?? []).Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        if (tipo == TipoCampoPersonalizado.Lista && opcoesLista.Count == 0)
            return Result.Failure<CampoPersonalizado>("Campo do tipo lista exige ao menos uma opção.");

        if (tipo != TipoCampoPersonalizado.Lista && opcoesLista.Count > 0)
            return Result.Failure<CampoPersonalizado>("Opções só fazem sentido para campo do tipo lista.");

        return Result.Success(new CampoPersonalizado(
            Guid.NewGuid(), tenantId, tipoProcessoId, nome.Trim(), tipo, opcoesLista, obrigatorio, ordem));
    }

    public Result AtualizarDados(string? nome, IEnumerable<string>? opcoes, bool obrigatorio, int ordem)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("O nome do campo é obrigatório.");

        var opcoesLista = (opcoes ?? []).Where(o => !string.IsNullOrWhiteSpace(o)).Select(o => o.Trim()).ToList();

        if (Tipo == TipoCampoPersonalizado.Lista && opcoesLista.Count == 0)
            return Result.Failure("Campo do tipo lista exige ao menos uma opção.");

        if (Tipo != TipoCampoPersonalizado.Lista && opcoesLista.Count > 0)
            return Result.Failure("Opções só fazem sentido para campo do tipo lista.");

        Nome = nome.Trim();
        Opcoes = opcoesLista;
        Obrigatorio = obrigatorio;
        Ordem = ordem;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }
}
