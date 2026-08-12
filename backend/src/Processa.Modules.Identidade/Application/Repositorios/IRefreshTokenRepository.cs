using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application.Repositorios;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default);

    /// <summary>Busca pelo hash do token apresentado (nunca pelo texto puro).</summary>
    Task<RefreshToken?> ObterPorHashAsync(string tokenHash, CancellationToken ct = default);
}
