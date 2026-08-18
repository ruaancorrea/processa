using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class TipoProcessoRepository(ProcessosDbContext db) : ITipoProcessoRepository
{
    public async Task AddAsync(TipoProcesso tipoProcesso, CancellationToken ct = default) =>
        await db.TiposProcesso.AddAsync(tipoProcesso, ct);

    public Task<TipoProcesso?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.TiposProcesso.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<List<TipoProcesso>> ListarAsync(CancellationToken ct = default) =>
        db.TiposProcesso.OrderBy(t => t.Nome).ToListAsync(ct);
}
