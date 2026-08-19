using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class DesdobramentoAguardadoRepository(ProcessosDbContext db) : IDesdobramentoAguardadoRepository
{
    public async Task AddAsync(DesdobramentoAguardado desdobramento, CancellationToken ct = default) =>
        await db.DesdobramentosAguardados.AddAsync(desdobramento, ct);

    public Task<List<DesdobramentoAguardado>> ListarPorExecucaoUniaoAsync(Guid execucaoEtapaUniaoId, CancellationToken ct = default) =>
        db.DesdobramentosAguardados.Where(d => d.ExecucaoEtapaUniaoId == execucaoEtapaUniaoId).ToListAsync(ct);

    public Task<DesdobramentoAguardado?> ObterPorExecucaoCondicionalAsync(Guid execucaoEtapaCondicionalId, CancellationToken ct = default) =>
        db.DesdobramentosAguardados.FirstOrDefaultAsync(d => d.ExecucaoEtapaCondicionalId == execucaoEtapaCondicionalId, ct);
}
