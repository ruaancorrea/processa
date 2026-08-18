using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Agrupamento linear de etapas (etapas em si nascem no Sprint 4). Exatamente um
/// fluxo é o padrão por tipo de processo — a unicidade é responsabilidade da
/// Application layer (índice único parcial no banco é o backstop real, ver
/// FluxoConfiguration), não desta entidade isolada: marcar UM fluxo como padrão
/// exige desmarcar os outros, uma operação que atravessa múltiplas linhas.
/// </summary>
public sealed class Fluxo : Entity
{
    public Guid TenantId { get; private set; }
    public Guid TipoProcessoId { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public bool FluxoPadrao { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Fluxo()
    {
        Nome = string.Empty;
    }

    private Fluxo(Guid id, Guid tenantId, Guid tipoProcessoId, string nome, string? descricao, bool fluxoPadrao) : base(id)
    {
        TenantId = tenantId;
        TipoProcessoId = tipoProcessoId;
        Nome = nome;
        Descricao = descricao;
        FluxoPadrao = fluxoPadrao;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Fluxo> Criar(Guid tenantId, Guid tipoProcessoId, string? nome, string? descricao, bool fluxoPadrao)
    {
        if (tenantId == Guid.Empty || tipoProcessoId == Guid.Empty)
            return Result.Failure<Fluxo>("Tenant e tipo de processo são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Fluxo>("O nome do fluxo é obrigatório.");

        return Result.Success(new Fluxo(Guid.NewGuid(), tenantId, tipoProcessoId, nome.Trim(), descricao?.Trim(), fluxoPadrao));
    }

    public Result AtualizarDados(string? nome, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("O nome do fluxo é obrigatório.");

        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void MarcarComoPadrao()
    {
        FluxoPadrao = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void DesmarcarComoPadrao()
    {
        FluxoPadrao = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
