using Microsoft.EntityFrameworkCore;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure;

public sealed class ConsultaCliente(ClientesDbContext db) : IConsultaCliente
{
    public async Task<IReadOnlyDictionary<Guid, string>> ObterRazoesSociaisAsync(IEnumerable<Guid> clienteIds, CancellationToken ct = default)
    {
        var ids = clienteIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await db.Clientes
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.RazaoSocial })
            .ToDictionaryAsync(c => c.Id, c => c.RazaoSocial, ct);
    }
}
