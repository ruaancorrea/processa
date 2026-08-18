using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IFluxoRepository
{
    Task AddAsync(Fluxo fluxo, CancellationToken ct = default);
    Task<Fluxo?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Fluxo>> ListarPorTipoProcessoAsync(Guid tipoProcessoId, CancellationToken ct = default);
    Task<Fluxo?> ObterPadraoAsync(Guid tipoProcessoId, CancellationToken ct = default);
    void Remover(Fluxo fluxo);
}
