using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IHistoricoExecucaoEtapaRepository
{
    Task AddAsync(HistoricoExecucaoEtapa historico, CancellationToken ct = default);
    Task<List<HistoricoExecucaoEtapa>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default);
}
