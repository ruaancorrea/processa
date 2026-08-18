using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Fluxos;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class CriarFluxoCommandHandlerTests
{
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarFluxoCommandHandler CriarHandler() =>
        new(_fluxoRepository, _tipoProcessoRepository, _tenantContext, _unitOfWork);

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

    public CriarFluxoCommandHandlerTests() => _tenantContext.TenantId.Returns(Guid.NewGuid());

    [Fact]
    public async Task Handle_TipoProcessoNaoExiste_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);

        var resultado = await CriarHandler().Handle(
            new CriarFluxoCommand(Guid.NewGuid(), "Fluxo", null, false), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PrimeiroFluxoDoTipoProcesso_ViraPadraoMesmoSemSolicitar()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _fluxoRepository.ObterPadraoAsync(tipoProcesso.Id).Returns((Fluxo?)null);

        Fluxo? adicionado = null;
        await _fluxoRepository.AddAsync(Arg.Do<Fluxo>(f => adicionado = f), Arg.Any<CancellationToken>());

        var resultado = await CriarHandler().Handle(
            new CriarFluxoCommand(tipoProcesso.Id, "Fluxo único", null, false), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        adicionado!.FluxoPadrao.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SegundoFluxoSemSolicitarPadrao_NaoViraPadraoENaoAlteraOAtual()
    {
        var tipoProcesso = CriarTipoProcesso();
        var fluxoAtual = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo atual", null, true).Value;
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _fluxoRepository.ObterPadraoAsync(tipoProcesso.Id).Returns(fluxoAtual);

        Fluxo? adicionado = null;
        await _fluxoRepository.AddAsync(Arg.Do<Fluxo>(f => adicionado = f), Arg.Any<CancellationToken>());

        var resultado = await CriarHandler().Handle(
            new CriarFluxoCommand(tipoProcesso.Id, "Fluxo secundário", null, false), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        adicionado!.FluxoPadrao.Should().BeFalse();
        fluxoAtual.FluxoPadrao.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_SegundoFluxoSolicitandoPadrao_DesmarcaOAtualEViraOPadrao()
    {
        var tipoProcesso = CriarTipoProcesso();
        var fluxoAtual = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo atual", null, true).Value;
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _fluxoRepository.ObterPadraoAsync(tipoProcesso.Id).Returns(fluxoAtual);

        Fluxo? adicionado = null;
        await _fluxoRepository.AddAsync(Arg.Do<Fluxo>(f => adicionado = f), Arg.Any<CancellationToken>());

        var resultado = await CriarHandler().Handle(
            new CriarFluxoCommand(tipoProcesso.Id, "Novo padrão", null, true), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        adicionado!.FluxoPadrao.Should().BeTrue();
        fluxoAtual.FluxoPadrao.Should().BeFalse();
    }
}

public class AtualizarFluxoCommandHandlerTests
{
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_FluxoExiste_Atualiza()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, false).Value;
        _fluxoRepository.ObterPorIdAsync(fluxo.Id).Returns(fluxo);
        var handler = new AtualizarFluxoCommandHandler(_fluxoRepository, _unitOfWork);

        var resultado = await handler.Handle(new AtualizarFluxoCommand(fluxo.Id, "Nome novo", null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        fluxo.Nome.Should().Be("Nome novo");
    }

    [Fact]
    public async Task Handle_FluxoNaoEncontrado_RetornaFalha()
    {
        _fluxoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Fluxo?)null);
        var handler = new AtualizarFluxoCommandHandler(_fluxoRepository, _unitOfWork);

        var resultado = await handler.Handle(new AtualizarFluxoCommand(Guid.NewGuid(), "Nome", null), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class DefinirFluxoPadraoCommandHandlerTests
{
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DefinirFluxoPadraoCommandHandler CriarHandler() => new(_fluxoRepository, _unitOfWork);

    [Fact]
    public async Task Handle_FluxoJaEhPadrao_NaoFazNada()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Fluxo", null, true).Value;
        _fluxoRepository.ObterPorIdAsync(fluxo.Id).Returns(fluxo);

        var resultado = await CriarHandler().Handle(new DefinirFluxoPadraoCommand(fluxo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PromoveOutroFluxo_DesmarcaOAntigoEMarcaONovo()
    {
        var tenantId = Guid.NewGuid();
        var tipoProcessoId = Guid.NewGuid();
        var fluxoAtual = Fluxo.Criar(tenantId, tipoProcessoId, "Atual", null, true).Value;
        var novoFluxo = Fluxo.Criar(tenantId, tipoProcessoId, "Novo", null, false).Value;
        _fluxoRepository.ObterPorIdAsync(novoFluxo.Id).Returns(novoFluxo);
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxoAtual);

        var resultado = await CriarHandler().Handle(new DefinirFluxoPadraoCommand(novoFluxo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        novoFluxo.FluxoPadrao.Should().BeTrue();
        fluxoAtual.FluxoPadrao.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_FluxoNaoEncontrado_RetornaFalha()
    {
        _fluxoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Fluxo?)null);

        var resultado = await CriarHandler().Handle(new DefinirFluxoPadraoCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class RemoverFluxoCommandHandlerTests
{
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RemoverFluxoCommandHandler CriarHandler() => new(_fluxoRepository, _unitOfWork);

    [Fact]
    public async Task Handle_FluxoNaoPadrao_Remove()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Fluxo", null, false).Value;
        _fluxoRepository.ObterPorIdAsync(fluxo.Id).Returns(fluxo);

        var resultado = await CriarHandler().Handle(new RemoverFluxoCommand(fluxo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _fluxoRepository.Received(1).Remover(fluxo);
    }

    [Fact]
    public async Task Handle_FluxoPadraoComOutrosFluxosExistindo_RetornaFalha()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxoPadrao = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Padrão", null, true).Value;
        var outroFluxo = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Outro", null, false).Value;
        _fluxoRepository.ObterPorIdAsync(fluxoPadrao.Id).Returns(fluxoPadrao);
        _fluxoRepository.ListarPorTipoProcessoAsync(tipoProcessoId).Returns([fluxoPadrao, outroFluxo]);

        var resultado = await CriarHandler().Handle(new RemoverFluxoCommand(fluxoPadrao.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        _fluxoRepository.DidNotReceive().Remover(Arg.Any<Fluxo>());
    }

    [Fact]
    public async Task Handle_FluxoPadraoUnico_Remove()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxoPadrao = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Único", null, true).Value;
        _fluxoRepository.ObterPorIdAsync(fluxoPadrao.Id).Returns(fluxoPadrao);
        _fluxoRepository.ListarPorTipoProcessoAsync(tipoProcessoId).Returns([fluxoPadrao]);

        var resultado = await CriarHandler().Handle(new RemoverFluxoCommand(fluxoPadrao.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _fluxoRepository.Received(1).Remover(fluxoPadrao);
    }

    [Fact]
    public async Task Handle_FluxoNaoEncontrado_RetornaFalha()
    {
        _fluxoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Fluxo?)null);

        var resultado = await CriarHandler().Handle(new RemoverFluxoCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
