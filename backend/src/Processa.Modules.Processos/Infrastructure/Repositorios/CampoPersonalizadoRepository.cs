using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class CampoPersonalizadoRepository(ProcessosDbContext db) : ICampoPersonalizadoRepository
{
    public async Task AddAsync(CampoPersonalizado campo, CancellationToken ct = default) =>
        await db.CamposPersonalizados.AddAsync(campo, ct);

    public Task<CampoPersonalizado?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.CamposPersonalizados.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<CampoPersonalizado>> ListarPorTipoProcessoAsync(Guid tipoProcessoId, CancellationToken ct = default) =>
        db.CamposPersonalizados.Where(c => c.TipoProcessoId == tipoProcessoId).OrderBy(c => c.Ordem).ToListAsync(ct);

    public void Remover(CampoPersonalizado campo) => db.CamposPersonalizados.Remove(campo);
}
