using Microsoft.EntityFrameworkCore;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Repositorios;

public sealed class GrupoClienteRepository(ClientesDbContext db) : IGrupoClienteRepository
{
    public async Task AddAsync(GrupoCliente grupo, CancellationToken ct = default) =>
        await db.GruposCliente.AddAsync(grupo, ct);

    public Task<GrupoCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.GruposCliente.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<List<GrupoCliente>> ListarAsync(CancellationToken ct = default) =>
        db.GruposCliente.OrderBy(g => g.Nome).ToListAsync(ct);
}
