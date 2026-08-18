using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Adapter deliberadamente não implementado — ler o valor de um campo preenchido
/// numa demanda real só faz sentido a partir do Sprint 5 (Execução de Demandas),
/// quando dados_execucao existir. Registrado agora só pra EtapaHandlerFactory
/// resolver de verdade em DI; falha alto (não silencioso) se for chamado antes
/// da hora, em vez de devolver um valor arbitrário que mascararia o problema.
/// </summary>
public sealed class ResolvedorValorCampoNaoImplementado : IResolvedorValorCampo
{
    public Task<string?> ObterValorAsync(Guid tenantId, Guid demandaId, Guid campoPersonalizadoId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException(
            "Leitura de valor de campo preenchido em demanda real é escopo do Sprint 5 (Execução de Demandas).");
}
