using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Application.Repositorios;

public interface IGrupoClienteRepository
{
    Task AddAsync(GrupoCliente grupo, CancellationToken ct = default);
    Task<GrupoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<GrupoCliente>> ListarAsync(CancellationToken ct = default);
}
