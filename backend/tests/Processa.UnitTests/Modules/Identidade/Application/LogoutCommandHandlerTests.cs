using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Auth;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LogoutCommandHandler CriarHandler() => new(_refreshTokenRepository, _tokenGenerator, _unitOfWork);

    [Fact]
    public async Task Handle_TokenValido_Revoga()
    {
        var token = RefreshToken.Criar(Guid.NewGuid(), "hash");
        _tokenGenerator.HashDoRefreshToken(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.ObterPorHashAsync("hash").Returns(token);

        await CriarHandler().Handle(new LogoutCommand("token-opaco"), CancellationToken.None);

        token.EhValido().Should().BeFalse();
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TokenInexistente_EhIdempotenteNaoLancaNemSalva()
    {
        _tokenGenerator.HashDoRefreshToken(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.ObterPorHashAsync("hash").Returns((RefreshToken?)null);

        var act = async () => await CriarHandler().Handle(new LogoutCommand("token-ja-invalido"), CancellationToken.None);

        await act.Should().NotThrowAsync();
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }
}
