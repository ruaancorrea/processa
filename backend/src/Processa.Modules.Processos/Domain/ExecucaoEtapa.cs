using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Uma passagem de uma Demanda por uma Etapa específica do fluxo. DadosExecucao guarda
/// os valores de campos personalizados preenchidos aqui (chave = CampoPersonalizado.Id
/// em string) — é o que IResolvedorValorCampo (Sprint 4) lê pra decidir ramos condicionais.
/// </summary>
public sealed class ExecucaoEtapa : Entity
{
    public Guid TenantId { get; private set; }
    public Guid DemandaId { get; private set; }
    public Guid EtapaId { get; private set; }
    public Guid? ResponsavelId { get; private set; }
    public StatusExecucaoEtapa Status { get; private set; }
    public DateTimeOffset IniciadoEm { get; private set; }
    public DateTimeOffset? ConcluidoEm { get; private set; }
    public IReadOnlyDictionary<string, string> DadosExecucao { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ExecucaoEtapa()
    {
        DadosExecucao = new Dictionary<string, string>();
    }

    private ExecucaoEtapa(Guid id, Guid tenantId, Guid demandaId, Guid etapaId, Guid? responsavelId) : base(id)
    {
        TenantId = tenantId;
        DemandaId = demandaId;
        EtapaId = etapaId;
        ResponsavelId = responsavelId;
        Status = StatusExecucaoEtapa.Pendente;
        IniciadoEm = DateTimeOffset.UtcNow;
        DadosExecucao = new Dictionary<string, string>();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<ExecucaoEtapa> Criar(Guid tenantId, Guid demandaId, Guid etapaId, Guid? responsavelId)
    {
        if (tenantId == Guid.Empty || demandaId == Guid.Empty || etapaId == Guid.Empty)
            return Result.Failure<ExecucaoEtapa>("Tenant, demanda e etapa são obrigatórios.");

        return Result.Success(new ExecucaoEtapa(Guid.NewGuid(), tenantId, demandaId, etapaId, responsavelId));
    }

    public void Iniciar()
    {
        if (Status == StatusExecucaoEtapa.Pendente)
            Status = StatusExecucaoEtapa.EmAndamento;
    }

    public Result Concluir()
    {
        if (Status is StatusExecucaoEtapa.Concluida or StatusExecucaoEtapa.Pulada)
            return Result.Failure("Execução já está concluída ou pulada.");

        Status = StatusExecucaoEtapa.Concluida;
        ConcluidoEm = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public Result Pular()
    {
        if (Status is StatusExecucaoEtapa.Concluida or StatusExecucaoEtapa.Pulada)
            return Result.Failure("Execução já está concluída ou pulada.");

        Status = StatusExecucaoEtapa.Pulada;
        ConcluidoEm = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void MarcarAguardando() => Status = StatusExecucaoEtapa.Aguardando;

    public void RegistrarErro() => Status = StatusExecucaoEtapa.Erro;

    public Result Reabrir()
    {
        if (Status != StatusExecucaoEtapa.Concluida)
            return Result.Failure("Só é possível reabrir uma execução concluída.");

        Status = StatusExecucaoEtapa.EmAndamento;
        ConcluidoEm = null;
        return Result.Success();
    }

    public void AtribuirResponsavel(Guid responsavelId) => ResponsavelId = responsavelId;

    public void DefinirValorCampo(Guid campoPersonalizadoId, string valor) => DefinirDado(campoPersonalizadoId.ToString(), valor);

    /// <summary>Chave livre — usado pelo orquestrador pra guardar metadado próprio (ex.: id da Demanda filha de um Subprocesso), não só valor de campo personalizado.</summary>
    public void DefinirDado(string chave, string valor)
    {
        var dados = new Dictionary<string, string>(DadosExecucao) { [chave] = valor };
        DadosExecucao = dados;
    }

    /// <summary>Adicionar comentário/anexo só faz sentido enquanto a execução ainda está em curso (ver requisitos, módulo 10.3).</summary>
    public bool AceitaNovaInteracao() => Status is StatusExecucaoEtapa.Pendente or StatusExecucaoEtapa.EmAndamento or StatusExecucaoEtapa.Aguardando;
}
