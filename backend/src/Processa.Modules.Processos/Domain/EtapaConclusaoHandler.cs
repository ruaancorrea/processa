namespace Processa.Modules.Processos.Domain;

/// <summary>Marca o fim do fluxo — o orquestrador (Sprint 5) decide o que fazer com a Demanda ao ver esse desfecho.</summary>
public sealed class EtapaConclusaoHandler : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Conclusao;

    public Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, Motivo: "Fim do fluxo."));
}
