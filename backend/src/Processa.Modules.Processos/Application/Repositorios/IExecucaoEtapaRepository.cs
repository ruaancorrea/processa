using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IExecucaoEtapaRepository
{
    Task AddAsync(ExecucaoEtapa execucao, CancellationToken ct = default);
    Task<ExecucaoEtapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ExecucaoEtapa>> ListarPorDemandaAsync(Guid demandaId, CancellationToken ct = default);
    Task<ExecucaoEtapa?> ObterPorDemandaEEtapaAsync(Guid demandaId, Guid etapaId, CancellationToken ct = default);
}
