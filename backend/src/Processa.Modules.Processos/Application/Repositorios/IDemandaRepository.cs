using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IDemandaRepository
{
    Task AddAsync(Demanda demanda, CancellationToken ct = default);
    Task<Demanda?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Demanda>> ListarAsync(CancellationToken ct = default);
    Task<int> ContarAtivasPorResponsavelAsync(Guid responsavelId, CancellationToken ct = default);
}
