using Microsoft.EntityFrameworkCore;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class ConsultaUsuario(IdentidadeDbContext db) : IConsultaUsuario
{
    public async Task<IReadOnlyDictionary<Guid, string>> ObterNomesAsync(IEnumerable<Guid> usuarioIds, CancellationToken ct = default)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await db.Usuarios
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nome })
            .ToDictionaryAsync(u => u.Id, u => u.Nome, ct);
    }
}
