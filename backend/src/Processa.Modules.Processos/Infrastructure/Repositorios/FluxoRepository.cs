using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class FluxoRepository(ProcessosDbContext db) : IFluxoRepository
{
    public async Task AddAsync(Fluxo fluxo, CancellationToken ct = default) =>
        await db.Fluxos.AddAsync(fluxo, ct);

    public Task<Fluxo?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Fluxos.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<List<Fluxo>> ListarPorTipoProcessoAsync(Guid tipoProcessoId, CancellationToken ct = default) =>
        db.Fluxos.Where(f => f.TipoProcessoId == tipoProcessoId).OrderBy(f => f.Nome).ToListAsync(ct);

    public Task<Fluxo?> ObterPadraoAsync(Guid tipoProcessoId, CancellationToken ct = default) =>
        db.Fluxos.FirstOrDefaultAsync(f => f.TipoProcessoId == tipoProcessoId && f.FluxoPadrao, ct);

    public void Remover(Fluxo fluxo) => db.Fluxos.Remove(fluxo);
}
