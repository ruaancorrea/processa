using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Etapas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class CriarEtapaCommandHandlerTests
{
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarEtapaCommandHandler CriarHandler() => new(_etapaRepository, _fluxoRepository, _tenantContext, _unitOfWork);

    private static Fluxo CriarFluxo() => Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Fluxo", null, true).Value;

    public CriarEtapaCommandHandlerTests() => _tenantContext.TenantId.Returns(Guid.NewGuid());

    [Fact]
    public async Task Handle_FluxoNaoExiste_RetornaFalha()
    {
        _fluxoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Fluxo?)null);

        var resultado = await CriarHandler().Handle(
            new CriarEtapaCommand(Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TipoComumSemConfiguracao_CriaEtapa()
    {
        var fluxo = CriarFluxo();
        _fluxoRepository.ObterPorIdAsync(fluxo.Id).Returns(fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarEtapaCommand(fluxo.Id, "Revisão manual", null, TipoEtapa.Comum, 0, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _etapaRepository.Received(1).AddAsync(Arg.Any<Etapa>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConfiguracaoIncompativelComTipo_RetornaFalha()
    {
        var fluxo = CriarFluxo();
        _fluxoRepository.ObterPorIdAsync(fluxo.Id).Returns(fluxo);

        var resultado = await CriarHandler().Handle(
            new CriarEtapaCommand(fluxo.Id, "Nome", null, TipoEtapa.Comum, 0, new ConfiguracaoEtapaAgendamento(1)), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _etapaRepository.DidNotReceive().AddAsync(Arg.Any<Etapa>(), Arg.Any<CancellationToken>());
    }
}

public class AtualizarEtapaCommandHandlerTests
{
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AtualizarEtapaCommandHandler CriarHandler() => new(_etapaRepository, _unitOfWork);

    private static Etapa CriarEtapa() =>
        Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, TipoEtapa.Comum, 0, null).Value;

    [Fact]
    public async Task Handle_EtapaNaoEncontrada_RetornaFalha()
    {
        _etapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Etapa?)null);

        var resultado = await CriarHandler().Handle(
            new AtualizarEtapaCommand(Guid.NewGuid(), "Nome", null, 0, null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DadosValidos_Atualiza()
    {
        var etapa = CriarEtapa();
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);

        var resultado = await CriarHandler().Handle(
            new AtualizarEtapaCommand(etapa.Id, "Nome novo", "Descrição", 1, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        etapa.Nome.Should().Be("Nome novo");
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }
}

public class RemoverEtapaCommandHandlerTests
{
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_EtapaExiste_Remove()
    {
        var etapa = Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, null).Value;
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);
        var handler = new RemoverEtapaCommandHandler(_etapaRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverEtapaCommand(etapa.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _etapaRepository.Received(1).Remover(etapa);
    }

    [Fact]
    public async Task Handle_EtapaNaoEncontrada_RetornaFalha()
    {
        _etapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Etapa?)null);
        var handler = new RemoverEtapaCommandHandler(_etapaRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverEtapaCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class DefinirConfiguracaoAcessoEtapaCommandHandlerTests
{
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IVerificadorUsuario _verificadorUsuario = Substitute.For<IVerificadorUsuario>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DefinirConfiguracaoAcessoEtapaCommandHandler CriarHandler() => new(_etapaRepository, _verificadorUsuario, _unitOfWork);

    private static Etapa CriarEtapa() => Etapa.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, TipoEtapa.Comum, 0, null).Value;

    [Fact]
    public async Task Handle_UsuarioExiste_DefineConfiguracaoAcesso()
    {
        var etapa = CriarEtapa();
        var usuarioId = Guid.NewGuid();
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);
        _verificadorUsuario.ExisteAsync(usuarioId).Returns(true);

        var resultado = await CriarHandler().Handle(
            new DefinirConfiguracaoAcessoEtapaCommand(etapa.Id, [Perfil.Gestor], [usuarioId]), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        etapa.ConfiguracaoAcesso.UsuarioIdsPodeAlterar.Should().Contain(usuarioId);
    }

    [Fact]
    public async Task Handle_UsuarioNaoExiste_RetornaFalha()
    {
        var etapa = CriarEtapa();
        var usuarioId = Guid.NewGuid();
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);
        _verificadorUsuario.ExisteAsync(usuarioId).Returns(false);

        var resultado = await CriarHandler().Handle(
            new DefinirConfiguracaoAcessoEtapaCommand(etapa.Id, [], [usuarioId]), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ListasNulas_NaoLancaExcecaoELimpaConfiguracao()
    {
        var etapa = CriarEtapa();
        etapa.DefinirConfiguracaoAcesso(ConfiguracaoAcessoEtapa.Criar([Perfil.Admin], []));
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);

        var resultado = await CriarHandler().Handle(new DefinirConfiguracaoAcessoEtapaCommand(etapa.Id, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        etapa.ConfiguracaoAcesso.Should().Be(ConfiguracaoAcessoEtapa.Vazia);
    }
}

public class EtapaQueriesTests
{
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();

    [Fact]
    public async Task ListarEtapas_RetornaResumoOrdenadoPorOrdem()
    {
        var fluxoId = Guid.NewGuid();
        var etapa1 = Etapa.Criar(Guid.NewGuid(), fluxoId, "Segunda", null, TipoEtapa.Comum, 1, null).Value;
        var etapa2 = Etapa.Criar(Guid.NewGuid(), fluxoId, "Primeira", null, TipoEtapa.Comum, 0, null).Value;
        _etapaRepository.ListarPorFluxoAsync(fluxoId).Returns([etapa1, etapa2]);
        var handler = new ListarEtapasQueryHandler(_etapaRepository);

        var resultado = await handler.Handle(new ListarEtapasQuery(fluxoId), CancellationToken.None);

        resultado.Select(e => e.Nome).Should().ContainInOrder("Primeira", "Segunda");
    }

    [Fact]
    public async Task ObterEtapaPorId_Existe_RetornaDetalhe()
    {
        var etapa = Etapa.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Chamada externa", null, TipoEtapa.Automatizada, 0,
            new ConfiguracaoEtapaAutomatizada("https://exemplo.com", MetodoHttp.Get, null, 200)).Value;
        _etapaRepository.ObterPorIdAsync(etapa.Id).Returns(etapa);
        var handler = new ObterEtapaPorIdQueryHandler(_etapaRepository);

        var resultado = await handler.Handle(new ObterEtapaPorIdQuery(etapa.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Configuracao.Should().BeOfType<ConfiguracaoEtapaAutomatizada>();
    }

    [Fact]
    public async Task ObterEtapaPorId_NaoEncontrada_RetornaFalha()
    {
        _etapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Etapa?)null);
        var handler = new ObterEtapaPorIdQueryHandler(_etapaRepository);

        var resultado = await handler.Handle(new ObterEtapaPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
