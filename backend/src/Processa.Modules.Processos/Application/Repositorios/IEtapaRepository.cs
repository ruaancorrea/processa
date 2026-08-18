using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IEtapaRepository
{
    Task AddAsync(Etapa etapa, CancellationToken ct = default);
    Task<Etapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Etapa>> ListarPorFluxoAsync(Guid fluxoId, CancellationToken ct = default);
    void Remover(Etapa etapa);
}
