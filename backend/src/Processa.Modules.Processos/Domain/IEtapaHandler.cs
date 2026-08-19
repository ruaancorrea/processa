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

public sealed record ExecucaoEtapaContexto(
    Guid TenantId, Guid DemandaId, Guid ExecucaoEtapaId, string ConfiguracaoJson, DateTimeOffset? IniciadoEm = null);

public enum DesfechoExecucao
{
    Concluida,
    Aguardando,
    Travada,
}

/// <summary>
/// ProximasEtapasIds tem mais de um elemento só quando uma Condicional dispara fork
/// real (mais de um ramo casou ao mesmo tempo) — o orquestrador cria uma ExecucaoEtapa
/// por id e avança cada ramo em paralelo, dentro da MESMA Demanda (ver
/// docs/03-modelagem/modelo-de-dados.md#desdobramentos_aguardados e Application/
/// Demandas/OrquestradorExecucao.cs). Lista vazia = "sem indicação de próxima etapa
/// específica" (o handler não decide, o orquestrador segue a ordem linear).
/// DadosResultantes é o único jeito de um handler devolver dado pro orquestrador
/// persistir em ExecucaoEtapa.DadosExecucao (ex.: EtapaSubprocessoHandler grava o id
/// da Demanda filha, pra o orquestrador saber qual ramo retomar quando ela concluir).
/// </summary>
public sealed record ResultadoExecucaoEtapa(
    DesfechoExecucao Desfecho,
    IReadOnlyList<Guid>? ProximasEtapasIds = null,
    string? Motivo = null,
    IReadOnlyDictionary<string, string>? DadosResultantes = null);
