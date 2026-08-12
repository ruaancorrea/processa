using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Infrastructure.Repositorios;

public sealed class RefreshTokenRepository(IdentidadeDbContext db) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
        await db.RefreshTokens.AddAsync(refreshToken, ct);

    public Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct = default) =>
        db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == tokenHash, ct);
}
