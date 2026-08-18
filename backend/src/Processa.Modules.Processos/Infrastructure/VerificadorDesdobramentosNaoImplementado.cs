using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Adapter deliberadamente não implementado — checar o estado real dos
/// desdobramentos_aguardados (fork/join) exige a tabela do Sprint 5. Ver
/// ResolvedorValorCampoNaoImplementado pra justificativa de falhar alto.
/// </summary>
public sealed class VerificadorDesdobramentosNaoImplementado : IVerificadorDesdobramentos
{
    public Task<bool> TodosConcluidosAsync(Guid tenantId, Guid execucaoEtapaUniaoId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Verificação real de desdobramentos aguardados (fork/join) é escopo do Sprint 5 (Execução de Demandas).");
}
