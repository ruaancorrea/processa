using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface ICampoPersonalizadoRepository
{
    Task AddAsync(CampoPersonalizado campo, CancellationToken ct = default);
    Task<CampoPersonalizado?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<CampoPersonalizado>> ListarPorTipoProcessoAsync(Guid tipoProcessoId, CancellationToken ct = default);
    void Remover(CampoPersonalizado campo);
}
