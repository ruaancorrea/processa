using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface ITipoProcessoRepository
{
    Task AddAsync(TipoProcesso tipoProcesso, CancellationToken ct = default);
    Task<TipoProcesso?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<TipoProcesso>> ListarAsync(CancellationToken ct = default);
}
