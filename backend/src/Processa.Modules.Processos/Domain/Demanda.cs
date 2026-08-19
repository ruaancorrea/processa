using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Instância de um TipoProcesso em execução — o que o usuário vê como "uma tarefa"
/// (ver requisitos, módulo 10). DemandaPaiId != null quando esta demanda nasceu de
/// uma Etapa de Subprocesso do processo pai.
/// </summary>
public sealed class Demanda : Entity
{
    public Guid TenantId { get; private set; }
    public Guid TipoProcessoId { get; private set; }
    public Guid FluxoAtivoId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public StatusDemanda Status { get; private set; }
    public Prioridade Prioridade { get; private set; }
    public int PercentualConclusao { get; private set; }
    public DateTimeOffset DataInicio { get; private set; }
    public DateTimeOffset? DataFimPrevista { get; private set; }
    public DateTimeOffset? DataFimReal { get; private set; }
    public Guid? EtapaAtualId { get; private set; }
    public Guid? DemandaPaiId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Demanda()
    {
    }

    private Demanda(
        Guid id, Guid tenantId, Guid tipoProcessoId, Guid fluxoAtivoId, Guid clienteId, Guid? responsavelId,
        Prioridade prioridade, DateTimeOffset? dataFimPrevista, Guid? demandaPaiId) : base(id)
    {
        TenantId = tenantId;
        TipoProcessoId = tipoProcessoId;
        FluxoAtivoId = fluxoAtivoId;
        ClienteId = clienteId;
        ResponsavelId = responsavelId;
        Status = responsavelId is null ? StatusDemanda.SemResponsavel : StatusDemanda.Pendente;
        Prioridade = prioridade;
        PercentualConclusao = 0;
        DataInicio = DateTimeOffset.UtcNow;
        DataFimPrevista = dataFimPrevista;
        DemandaPaiId = demandaPaiId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Demanda> Criar(
        Guid tenantId, Guid tipoProcessoId, Guid fluxoAtivoId, Guid clienteId, Guid? responsavelId,
        Prioridade prioridade, DateTimeOffset? dataFimPrevista, Guid? demandaPaiId = null)
    {
        if (tenantId == Guid.Empty || tipoProcessoId == Guid.Empty || fluxoAtivoId == Guid.Empty || clienteId == Guid.Empty)
            return Result.Failure<Demanda>("Tenant, tipo de processo, fluxo e cliente são obrigatórios.");

        return Result.Success(new Demanda(
            Guid.NewGuid(), tenantId, tipoProcessoId, fluxoAtivoId, clienteId, responsavelId, prioridade, dataFimPrevista, demandaPaiId));
    }

    public Result AtribuirResponsavel(Guid responsavelId)
    {
        if (responsavelId == Guid.Empty)
            return Result.Failure("O responsável é obrigatório.");
        if (Status is StatusDemanda.Concluido or StatusDemanda.Cancelado)
            return Result.Failure("Demanda já concluída ou cancelada não aceita novo responsável.");

        ResponsavelId = responsavelId;
        if (Status == StatusDemanda.SemResponsavel)
            Status = StatusDemanda.Pendente;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void IniciarEtapa(Guid etapaId)
    {
        EtapaAtualId = etapaId;
        if (Status == StatusDemanda.Pendente)
            Status = StatusDemanda.EmAndamento;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AtualizarPercentualConclusao(int percentual)
    {
        PercentualConclusao = Math.Clamp(percentual, 0, 100);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Result Concluir()
    {
        if (Status is StatusDemanda.Concluido or StatusDemanda.Cancelado)
            return Result.Failure("Demanda já está concluída ou cancelada.");

        Status = StatusDemanda.Concluido;
        PercentualConclusao = 100;
        DataFimReal = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Cancelar()
    {
        if (Status is StatusDemanda.Concluido or StatusDemanda.Cancelado)
            return Result.Failure("Demanda já está concluída ou cancelada.");

        Status = StatusDemanda.Cancelado;
        DataFimReal = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void AlterarPrioridade(Prioridade prioridade)
    {
        Prioridade = prioridade;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
