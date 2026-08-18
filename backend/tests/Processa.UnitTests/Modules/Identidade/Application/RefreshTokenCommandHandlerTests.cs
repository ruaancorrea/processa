using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Auth;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RefreshTokenCommandHandler CriarHandler() =>
        new(_refreshTokenRepository, _usuarioRepository, _tokenGenerator, _unitOfWork);

    [Fact]
    public async Task Handle_TokenInexistente_RetornaFalha()
    {
        _tokenGenerator.HashDoRefreshToken(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.ObterPorHashAsync("hash").Returns((RefreshToken?)null);

        var resultado = await CriarHandler().Handle(new RefreshTokenCommand("token-inexistente"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenJaRevogado_RetornaFalha()
    {
        // EhValido() é falso tanto para revogado quanto para expirado (mesmo branch no
        // handler); revogado é o caminho que dá para montar sem expor um setter de
        // ExpiraEm no domínio só para teste.
        var usuario = Usuario.Criar(Guid.NewGuid(), "Ana", "ana@exemplo.com", "hash", Perfil.Analista).Value;
        var tokenRevogado = RefreshToken.Criar(usuario.Id, "hash-velho");
        tokenRevogado.Revogar();

        _tokenGenerator.HashDoRefreshToken(Arg.Any<string>()).Returns("hash");
        _refreshTokenRepository.ObterPorHashAsync("hash").Returns(tokenRevogado);

        var resultado = await CriarHandler().Handle(new RefreshTokenCommand("token-velho"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TokenValido_RotacionaERetornaNovoAccessToken()
    {
        var usuario = Usuario.Criar(Guid.NewGuid(), "Ana", "ana@exemplo.com", "hash", Perfil.Analista).Value;
        var tokenValido = RefreshToken.Criar(usuario.Id, "hash-antigo");

        _tokenGenerator.HashDoRefreshToken("token-atual").Returns("hash-antigo");
        _refreshTokenRepository.ObterPorHashAsync("hash-antigo").Returns(tokenValido);
        _usuarioRepository.ObterPorIdIgnorandoTenantAsync(usuario.Id).Returns(usuario);
        _tokenGenerator.GerarAccessToken(usuario).Returns(new AccessTokenGerado("novo-jwt", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokenGenerator.GerarRefreshTokenOpaco().Returns("novo-refresh-opaco");
        _tokenGenerator.HashDoRefreshToken("novo-refresh-opaco").Returns("novo-hash");

        var resultado = await CriarHandler().Handle(new RefreshTokenCommand("token-atual"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.AccessToken.Should().Be("novo-jwt");
        tokenValido.EhValido().Should().BeFalse("o token apresentado deve ser revogado após o uso (rotação)");
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
