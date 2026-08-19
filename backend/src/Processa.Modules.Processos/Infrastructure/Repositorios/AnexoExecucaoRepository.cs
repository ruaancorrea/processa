using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class AnexoExecucaoRepository(ProcessosDbContext db) : IAnexoExecucaoRepository
{
    public async Task AddAsync(AnexoExecucao anexo, CancellationToken ct = default) =>
        await db.AnexosExecucao.AddAsync(anexo, ct);

    public Task<AnexoExecucao?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.AnexosExecucao.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<AnexoExecucao>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default) =>
        db.AnexosExecucao.Where(a => a.ExecucaoEtapaId == execucaoEtapaId).OrderBy(a => a.CreatedAt).ToListAsync(ct);
}
