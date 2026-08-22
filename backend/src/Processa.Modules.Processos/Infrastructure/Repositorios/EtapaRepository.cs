using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class EtapaRepository(ProcessosDbContext db) : IEtapaRepository
{
    public async Task AddAsync(Etapa etapa, CancellationToken ct = default) =>
        await db.Etapas.AddAsync(etapa, ct);

    public Task<Etapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Etapas.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<Etapa>> ListarPorFluxoAsync(Guid fluxoId, CancellationToken ct = default) =>
        db.Etapas.Where(e => e.FluxoId == fluxoId).OrderBy(e => e.Ordem).ToListAsync(ct);

    public Task<List<Etapa>> ListarPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return lista.Count == 0 ? Task.FromResult(new List<Etapa>()) : db.Etapas.Where(e => lista.Contains(e.Id)).ToListAsync(ct);
    }

    public void Remover(Etapa etapa) => db.Etapas.Remove(etapa);
}
