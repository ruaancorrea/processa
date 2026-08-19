using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class ExecucaoEtapaRepository(ProcessosDbContext db) : IExecucaoEtapaRepository
{
    public async Task AddAsync(ExecucaoEtapa execucao, CancellationToken ct = default) =>
        await db.ExecucoesEtapa.AddAsync(execucao, ct);

    public Task<ExecucaoEtapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.ExecucoesEtapa.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<ExecucaoEtapa>> ListarPorDemandaAsync(Guid demandaId, CancellationToken ct = default) =>
        db.ExecucoesEtapa.Where(e => e.DemandaId == demandaId).ToListAsync(ct);

    public Task<ExecucaoEtapa?> ObterPorDemandaEEtapaAsync(Guid demandaId, Guid etapaId, CancellationToken ct = default) =>
        db.ExecucoesEtapa.FirstOrDefaultAsync(e => e.DemandaId == demandaId && e.EtapaId == etapaId, ct);
}
