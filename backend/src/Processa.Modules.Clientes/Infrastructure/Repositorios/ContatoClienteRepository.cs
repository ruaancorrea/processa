using Microsoft.EntityFrameworkCore;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Repositorios;

public sealed class ContatoClienteRepository(ClientesDbContext db) : IContatoClienteRepository
{
    public async Task AddAsync(ContatoCliente contato, CancellationToken ct = default) =>
        await db.ContatosCliente.AddAsync(contato, ct);

    public Task<ContatoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.ContatosCliente.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<ContatoCliente>> ListarPorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.ContatosCliente.Where(c => c.ClienteId == clienteId).ToListAsync(ct);
}
