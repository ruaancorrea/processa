namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Strategy por tipo de etapa — cada implementação decide como uma execução
/// avança, trava ou aguarda. Resolvido em runtime por um EtapaHandlerFactory
/// na camada de Application. Ver ADR-003.
/// </summary>
public interface IEtapaHandler
{
    TipoEtapa Tipo { get; }

    Task<ResultadoExecucaoEtapa> ExecutarAsync(
        ExecucaoEtapaContexto contexto,
        CancellationToken cancellationToken = default);
}

public sealed record ExecucaoEtapaContexto(Guid TenantId, Guid DemandaId, Guid ExecucaoEtapaId, string ConfiguracaoJson);

public enum DesfechoExecucao
{
    Concluida,
    Aguardando,
    Travada,
}

public sealed record ResultadoExecucaoEtapa(DesfechoExecucao Desfecho, Guid? ProximaEtapaId = null, string? Motivo = null);
