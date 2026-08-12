using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;

namespace Processa.Modules.Identidade.Application.Auth;

public sealed record LogoutCommand(string RefreshTokenOpaco) : IRequest;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenGenerator.HashDoRefreshToken(request.RefreshTokenOpaco);
        var token = await refreshTokenRepository.ObterPorHashAsync(hash, cancellationToken);

        // Idempotente: token já ausente/expirado/revogado -> logout é um no-op silencioso.
        if (token is null || !token.EhValido())
            return;

        token.Revogar();
        await unitOfWork.SalvarAsync(cancellationToken);
    }
}
