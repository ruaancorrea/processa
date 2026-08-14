using Microsoft.EntityFrameworkCore;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Infrastructure.Repositorios;

public sealed class ResponsavelClienteRepository(ClientesDbContext db) : IResponsavelClienteRepository
{
    public async Task AddAsync(ResponsavelCliente responsavel, CancellationToken ct = default) =>
        await db.ResponsaveisCliente.AddAsync(responsavel, ct);

    public Task<ResponsavelCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.ResponsaveisCliente.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<ResponsavelCliente?> ObterAsync(Guid clienteId, Guid equipeId, Guid usuarioId, CancellationToken ct = default) =>
        db.ResponsaveisCliente.FirstOrDefaultAsync(
            r => r.ClienteId == clienteId && r.EquipeId == equipeId && r.UsuarioId == usuarioId, ct);

    public Task<List<ResponsavelCliente>> ListarAtivosPorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.ResponsaveisCliente.Where(r => r.ClienteId == clienteId && r.RemovidoEm == null).ToListAsync(ct);
}
