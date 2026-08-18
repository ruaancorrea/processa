using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Adapter deliberadamente não implementado — criar uma Demanda filha com herança
/// de campos personalizados do pai é escopo do Sprint 5. Ver
/// ResolvedorValorCampoNaoImplementado pra justificativa de falhar alto.
/// </summary>
public sealed class CriadorSubprocessoNaoImplementado : ICriadorSubprocesso
{
    public Task<Guid> CriarAsync(
        Guid tenantId, Guid demandaPaiId, Guid tipoProcessoFilhoId, bool herdarResponsavel, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Criação de subprocesso real (Demanda filha) é escopo do Sprint 5 (Execução de Demandas).");
}
