using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class ComentarioExecucaoRepository(ProcessosDbContext db) : IComentarioExecucaoRepository
{
    public async Task AddAsync(ComentarioExecucao comentario, CancellationToken ct = default) =>
        await db.ComentariosExecucao.AddAsync(comentario, ct);

    public Task<List<ComentarioExecucao>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default) =>
        db.ComentariosExecucao.Where(c => c.ExecucaoEtapaId == execucaoEtapaId).OrderBy(c => c.CreatedAt).ToListAsync(ct);
}
