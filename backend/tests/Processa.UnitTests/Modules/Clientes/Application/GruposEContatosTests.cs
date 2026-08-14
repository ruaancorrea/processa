using FluentAssertions;
using NSubstitute;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Contatos;
using Processa.Modules.Clientes.Application.Grupos;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Application.Responsaveis;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Application;

public class GrupoClienteCommandsTests
{
    private readonly IGrupoClienteRepository _grupoClienteRepository = Substitute.For<IGrupoClienteRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task CriarGrupo_DadosValidos_Cria()
    {
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        var handler = new CriarGrupoClienteCommandHandler(_grupoClienteRepository, _tenantContext, _unitOfWork);

        var resultado = await handler.Handle(new CriarGrupoClienteCommand("Grupo Varejo", null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _grupoClienteRepository.Received(1).AddAsync(Arg.Any<GrupoCliente>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListarGrupos_RetornaResumoDeTodos()
    {
        var grupo = GrupoCliente.Criar(Guid.NewGuid(), "Grupo Varejo", null).Value;
        _grupoClienteRepository.ListarAsync().Returns([grupo]);
        var handler = new ListarGruposClienteQueryHandler(_grupoClienteRepository);

        var resultado = await handler.Handle(new ListarGruposClienteQuery(), CancellationToken.None);

        resultado.Should().ContainSingle(g => g.Id == grupo.Id);
    }
}

public class ContatoStatusCommandsTests
{
    private readonly IContatoClienteRepository _contatoClienteRepository = Substitute.For<IContatoClienteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static ContatoCliente CriarContato() =>
        ContatoCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), "Maria", "maria@exemplo.com", null, null).Value;

    [Fact]
    public async Task DesativarContato_ContatoExiste_Desativa()
    {
        var contato = CriarContato();
        _contatoClienteRepository.ObterPorIdAsync(contato.Id).Returns(contato);
        var handler = new DesativarContatoCommandHandler(_contatoClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarContatoCommand(contato.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        contato.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task DesativarContato_ContatoNaoExiste_RetornaFalha()
    {
        _contatoClienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ContatoCliente?)null);
        var handler = new DesativarContatoCommandHandler(_contatoClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarContatoCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ReativarContato_ContatoExiste_Reativa()
    {
        var contato = CriarContato();
        contato.Desativar();
        _contatoClienteRepository.ObterPorIdAsync(contato.Id).Returns(contato);
        var handler = new ReativarContatoCommandHandler(_contatoClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new ReativarContatoCommand(contato.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        contato.Ativo.Should().BeTrue();
    }
}

public class RemoverResponsavelCommandHandlerTests
{
    private readonly IResponsavelClienteRepository _responsavelClienteRepository = Substitute.For<IResponsavelClienteRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_ResponsavelAtivoExiste_Remove()
    {
        var responsavel = ResponsavelCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        _responsavelClienteRepository.ObterPorIdAsync(responsavel.Id).Returns(responsavel);
        var handler = new RemoverResponsavelCommandHandler(_responsavelClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverResponsavelCommand(responsavel.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        responsavel.EstaAtivo.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ResponsavelNaoEncontrado_RetornaFalha()
    {
        _responsavelClienteRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ResponsavelCliente?)null);
        var handler = new RemoverResponsavelCommandHandler(_responsavelClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverResponsavelCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ResponsavelJaRemovido_RetornaFalha()
    {
        var responsavel = ResponsavelCliente.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;
        responsavel.Remover();
        _responsavelClienteRepository.ObterPorIdAsync(responsavel.Id).Returns(responsavel);
        var handler = new RemoverResponsavelCommandHandler(_responsavelClienteRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverResponsavelCommand(responsavel.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
