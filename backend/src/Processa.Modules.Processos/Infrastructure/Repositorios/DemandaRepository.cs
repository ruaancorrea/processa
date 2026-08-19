using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class DemandaRepository(ProcessosDbContext db) : IDemandaRepository
{
    public async Task AddAsync(Demanda demanda, CancellationToken ct = default) =>
        await db.Demandas.AddAsync(demanda, ct);

    public Task<Demanda?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Demandas.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<Demanda>> ListarAsync(CancellationToken ct = default) =>
        db.Demandas.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

    public Task<int> ContarAtivasPorResponsavelAsync(Guid responsavelId, CancellationToken ct = default) =>
        db.Demandas.CountAsync(
            d => d.ResponsavelId == responsavelId && d.Status != StatusDemanda.Concluido && d.Status != StatusDemanda.Cancelado, ct);
}
