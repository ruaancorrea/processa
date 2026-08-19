using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IAnexoExecucaoRepository
{
    Task AddAsync(AnexoExecucao anexo, CancellationToken ct = default);
    Task<AnexoExecucao?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<AnexoExecucao>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default);
}
