using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Auth;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class LoginCommandHandlerTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IRefreshTokenRepository _refreshTokenRepository = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private LoginCommandHandler CriarHandler() => new(
        _usuarioRepository, _refreshTokenRepository, _passwordHasher, _tokenGenerator, _unitOfWork);

    private static Usuario CriarUsuario() =>
        Usuario.Criar(Guid.NewGuid(), "Ana", "ana@exemplo.com", "hash-armazenado", Perfil.Analista).Value;

    [Fact]
    public async Task Handle_UsuarioNaoExiste_RetornaFalhaGenerica()
    {
        _usuarioRepository.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Usuario?)null);

        var resultado = await CriarHandler().Handle(new LoginCommand("ninguem@exemplo.com", "qualquer"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("E-mail ou senha inválidos.");
    }

    [Fact]
    public async Task Handle_UsuarioNaoExiste_AindaAssimRodaVerificacaoDeSenhaFicticia()
    {
        // Mitigação de timing attack: sem isso, a resposta para e-mail inexistente
        // volta bem mais rápido que para senha errada (que roda bcrypt de verdade),
        // permitindo enumerar e-mails cadastrados medindo a latência da resposta.
        _usuarioRepository.ObterPorEmailAsync(Arg.Any<Email>()).Returns((Usuario?)null);

        await CriarHandler().Handle(new LoginCommand("ninguem@exemplo.com", "qualquer"), CancellationToken.None);

        _passwordHasher.Received(1).Verificar(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_SenhaErrada_RegistraTentativaFalhaERetornaFalhaGenerica()
    {
        var usuario = CriarUsuario();
        _usuarioRepository.ObterPorEmailAsync(Arg.Any<Email>()).Returns(usuario);
        _passwordHasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var resultado = await CriarHandler().Handle(new LoginCommand("ana@exemplo.com", "senha-errada"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Be("E-mail ou senha inválidos.");
        usuario.TentativasLoginFalhas.Should().Be(1);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsuarioBloqueado_RetornaFalhaComMensagemDeBloqueio()
    {
        var usuario = CriarUsuario();
        for (var i = 0; i < 5; i++)
            usuario.RegistrarTentativaFalha();

        _usuarioRepository.ObterPorEmailAsync(Arg.Any<Email>()).Returns(usuario);

        var resultado = await CriarHandler().Handle(new LoginCommand("ana@exemplo.com", "qualquer"), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("bloqueada");
        // Não deve nem chegar a checar a senha — usuário bloqueado é a primeira barreira.
        _passwordHasher.DidNotReceive().Verificar(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_CredenciaisCorretas_RetornaSucessoEResetaTentativasFalhas()
    {
        var usuario = CriarUsuario();
        usuario.RegistrarTentativaFalha();
        usuario.RegistrarTentativaFalha();

        _usuarioRepository.ObterPorEmailAsync(Arg.Any<Email>()).Returns(usuario);
        _passwordHasher.Verificar(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _tokenGenerator.GerarAccessToken(usuario).Returns(new AccessTokenGerado("jwt-fake", DateTimeOffset.UtcNow.AddMinutes(15)));
        _tokenGenerator.GerarRefreshTokenOpaco().Returns("refresh-opaco");
        _tokenGenerator.HashDoRefreshToken("refresh-opaco").Returns("refresh-hash");

        var resultado = await CriarHandler().Handle(new LoginCommand("ana@exemplo.com", "senha-correta"), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.AccessToken.Should().Be("jwt-fake");
        resultado.Value.RefreshTokenOpaco.Should().Be("refresh-opaco");
        usuario.TentativasLoginFalhas.Should().Be(0);
        await _refreshTokenRepository.Received(1).AddAsync(Arg.Any<RefreshToken>(), Arg.Any<CancellationToken>());
    }
}
