using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class HistoricoExecucaoEtapaRepository(ProcessosDbContext db) : IHistoricoExecucaoEtapaRepository
{
    public async Task AddAsync(HistoricoExecucaoEtapa historico, CancellationToken ct = default) =>
        await db.HistoricosExecucaoEtapa.AddAsync(historico, ct);

    public Task<List<HistoricoExecucaoEtapa>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default) =>
        db.HistoricosExecucaoEtapa.Where(h => h.ExecucaoEtapaId == execucaoEtapaId).OrderBy(h => h.CreatedAt).ToListAsync(ct);
}
