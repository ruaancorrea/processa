using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Application.Repositorios;

public interface IContatoClienteRepository
{
    Task AddAsync(ContatoCliente contato, CancellationToken ct = default);
    Task<ContatoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ContatoCliente>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct = default);
}
