using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Repositorios;

public sealed class MembroEquipeRepository(IdentidadeDbContext db) : IMembroEquipeRepository
{
    public async Task AddAsync(MembroEquipe membro, CancellationToken ct = default) =>
        await db.MembrosEquipe.AddAsync(membro, ct);

    public Task<MembroEquipe?> ObterAsync(Guid equipeId, Guid usuarioId, CancellationToken ct = default) =>
        db.MembrosEquipe.FirstOrDefaultAsync(m => m.EquipeId == equipeId && m.UsuarioId == usuarioId, ct);

    public Task<List<MembroEquipe>> ListarPorEquipeAsync(Guid equipeId, CancellationToken ct = default) =>
        db.MembrosEquipe.Where(m => m.EquipeId == equipeId).ToListAsync(ct);

    public void Remover(MembroEquipe membro) => db.MembrosEquipe.Remove(membro);
}
