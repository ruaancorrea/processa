using FluentAssertions;
using NSubstitute;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Equipes;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Application;

public class AdicionarMembroCommandHandlerTests
{
    private readonly IEquipeRepository _equipeRepository = Substitute.For<IEquipeRepository>();
    private readonly IMembroEquipeRepository _membroEquipeRepository = Substitute.For<IMembroEquipeRepository>();
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AdicionarMembroCommandHandler CriarHandler() => new(
        _equipeRepository, _membroEquipeRepository, _usuarioRepository, _tenantContext, _unitOfWork);

    private static Equipe CriarEquipe() => Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", null).Value;

    private static Usuario CriarUsuario() =>
        Usuario.Criar(Guid.NewGuid(), "Ana", "ana@exemplo.com", "hash", Perfil.Analista).Value;

    [Fact]
    public async Task Handle_EquipeEUsuarioValidos_AdicionaMembro()
    {
        var equipe = CriarEquipe();
        var usuario = CriarUsuario();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        _usuarioRepository.ObterPorIdAsync(usuario.Id).Returns(usuario);
        _membroEquipeRepository.ObterAsync(equipe.Id, usuario.Id).Returns((MembroEquipe?)null);
        _tenantContext.TenantId.Returns(Guid.NewGuid());

        var resultado = await CriarHandler().Handle(
            new AdicionarMembroCommand(equipe.Id, usuario.Id, Perfil.Analista), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _membroEquipeRepository.Received(1).AddAsync(Arg.Any<MembroEquipe>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_EquipeNaoEncontrada_RetornaFalha()
    {
        _equipeRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Equipe?)null);

        var resultado = await CriarHandler().Handle(
            new AdicionarMembroCommand(Guid.NewGuid(), Guid.NewGuid(), Perfil.Analista), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Equipe");
    }

    [Fact]
    public async Task Handle_UsuarioNaoEncontrado_RetornaFalha()
    {
        var equipe = CriarEquipe();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        _usuarioRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Usuario?)null);

        var resultado = await CriarHandler().Handle(
            new AdicionarMembroCommand(equipe.Id, Guid.NewGuid(), Perfil.Analista), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().Contain("Usuário");
    }

    [Fact]
    public async Task Handle_UsuarioJaEhMembro_RetornaFalha()
    {
        var equipe = CriarEquipe();
        var usuario = CriarUsuario();
        _equipeRepository.ObterPorIdAsync(equipe.Id).Returns(equipe);
        _usuarioRepository.ObterPorIdAsync(usuario.Id).Returns(usuario);
        _membroEquipeRepository.ObterAsync(equipe.Id, usuario.Id)
            .Returns(MembroEquipe.Adicionar(Guid.NewGuid(), equipe.Id, usuario.Id, Perfil.Analista).Value);

        var resultado = await CriarHandler().Handle(
            new AdicionarMembroCommand(equipe.Id, usuario.Id, Perfil.Analista), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _membroEquipeRepository.DidNotReceive().AddAsync(Arg.Any<MembroEquipe>(), Arg.Any<CancellationToken>());
    }
}
