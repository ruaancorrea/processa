using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Repositorios;

public sealed class EquipeRepository(IdentidadeDbContext db) : IEquipeRepository
{
    public async Task AddAsync(Equipe equipe, CancellationToken ct = default) =>
        await db.Equipes.AddAsync(equipe, ct);

    public Task<Equipe?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Equipes.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<Equipe>> ListarAsync(CancellationToken ct = default) =>
        db.Equipes.OrderBy(e => e.Nome).ToListAsync(ct);
}
