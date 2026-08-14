using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Repositorios;

public interface IClienteRepository
{
    Task AddAsync(Cliente cliente, CancellationToken ct = default);
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExisteCnpjAsync(Cnpj cnpj, CancellationToken ct = default);
    Task<List<Cliente>> ListarAsync(CancellationToken ct = default);
}
