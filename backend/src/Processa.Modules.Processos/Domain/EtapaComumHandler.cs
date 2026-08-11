namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Etapa manual: conclui apenas quando o responsável marca explicitamente
/// (ação vinda da camada de Application, disparada pelo endpoint de conclusão).
/// </summary>
public sealed class EtapaComumHandler : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Comum;

    public Task<ResultadoExecucaoEtapa> ExecutarAsync(
        ExecucaoEtapaContexto contexto,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida));
    }
}
