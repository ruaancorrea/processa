using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Auth;

public sealed record RefreshTokenCommand(string RefreshTokenOpaco) : IRequest<Result<LoginResultado>>;

public sealed class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUsuarioRepository usuarioRepository,
    IJwtTokenGenerator tokenGenerator,
    IUnitOfWork unitOfWork) : IRequestHandler<RefreshTokenCommand, Result<LoginResultado>>
{
    public async Task<Result<LoginResultado>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenGenerator.HashDoRefreshToken(request.RefreshTokenOpaco);
        var tokenExistente = await refreshTokenRepository.ObterPorHashAsync(hash, cancellationToken);

        if (tokenExistente is null || !tokenExistente.EhValido())
            return Result.Failure<LoginResultado>("Sessão expirada. Faça login novamente.");

        var usuario = await usuarioRepository.ObterPorIdIgnorandoTenantAsync(tokenExistente.UsuarioId, cancellationToken);
        if (usuario is null || !usuario.Ativo)
            return Result.Failure<LoginResultado>("Sessão expirada. Faça login novamente.");

        // Rotação: o token apresentado é revogado e um novo é emitido a cada refresh
        // (mitiga replay de um refresh token vazado, mesmo sem reuse-detection completo
        // — ver .faf/decisions.faf e .faf/pendencias.faf).
        tokenExistente.Revogar();

        var accessToken = tokenGenerator.GerarAccessToken(usuario);
        var novoRefreshOpaco = tokenGenerator.GerarRefreshTokenOpaco();
        var novoRefreshHash = tokenGenerator.HashDoRefreshToken(novoRefreshOpaco);

        await refreshTokenRepository.AddAsync(
            Processa.Modules.Identidade.Domain.RefreshToken.Criar(usuario.Id, novoRefreshHash),
            cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(new LoginResultado(
            accessToken.Token,
            accessToken.ExpiraEm,
            novoRefreshOpaco,
            usuario.Id,
            usuario.TenantId,
            usuario.Perfil));
    }
}
