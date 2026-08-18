using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Campos;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class CriarCampoPersonalizadoCommandHandlerTests
{
    private readonly ICampoPersonalizadoRepository _campoPersonalizadoRepository = Substitute.For<ICampoPersonalizadoRepository>();
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly ITenantContext _tenantContext = Substitute.For<ITenantContext>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CriarCampoPersonalizadoCommandHandler CriarHandler() =>
        new(_campoPersonalizadoRepository, _tipoProcessoRepository, _tenantContext, _unitOfWork);

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

    [Fact]
    public async Task Handle_TipoProcessoExiste_CriaCampo()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);

        var resultado = await CriarHandler().Handle(
            new CriarCampoPersonalizadoCommand(tipoProcesso.Id, "Campo", TipoCampoPersonalizado.Texto, null, false, 0),
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        await _campoPersonalizadoRepository.Received(1).AddAsync(Arg.Any<CampoPersonalizado>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TipoProcessoNaoExiste_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);

        var resultado = await CriarHandler().Handle(
            new CriarCampoPersonalizadoCommand(Guid.NewGuid(), "Campo", TipoCampoPersonalizado.Texto, null, false, 0),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_TipoListaSemOpcoes_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tenantContext.TenantId.Returns(Guid.NewGuid());
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);

        var resultado = await CriarHandler().Handle(
            new CriarCampoPersonalizadoCommand(tipoProcesso.Id, "Campo", TipoCampoPersonalizado.Lista, null, false, 0),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class AtualizarCampoPersonalizadoCommandHandlerTests
{
    private readonly ICampoPersonalizadoRepository _campoPersonalizadoRepository = Substitute.For<ICampoPersonalizadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static CampoPersonalizado CriarCampo() =>
        CampoPersonalizado.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", TipoCampoPersonalizado.Numero, null, false, 0).Value;

    [Fact]
    public async Task Handle_CampoExiste_Atualiza()
    {
        var campo = CriarCampo();
        _campoPersonalizadoRepository.ObterPorIdAsync(campo.Id).Returns(campo);
        var handler = new AtualizarCampoPersonalizadoCommandHandler(_campoPersonalizadoRepository, _unitOfWork);

        var resultado = await handler.Handle(
            new AtualizarCampoPersonalizadoCommand(campo.Id, "Nome novo", null, true, 1), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        campo.Nome.Should().Be("Nome novo");
    }

    [Fact]
    public async Task Handle_CampoNaoEncontrado_RetornaFalha()
    {
        _campoPersonalizadoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((CampoPersonalizado?)null);
        var handler = new AtualizarCampoPersonalizadoCommandHandler(_campoPersonalizadoRepository, _unitOfWork);

        var resultado = await handler.Handle(
            new AtualizarCampoPersonalizadoCommand(Guid.NewGuid(), "Nome", null, false, 0), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class RemoverCampoPersonalizadoCommandHandlerTests
{
    private readonly ICampoPersonalizadoRepository _campoPersonalizadoRepository = Substitute.For<ICampoPersonalizadoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task Handle_CampoExiste_Remove()
    {
        var campo = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Campo", TipoCampoPersonalizado.Texto, null, false, 0).Value;
        _campoPersonalizadoRepository.ObterPorIdAsync(campo.Id).Returns(campo);
        var handler = new RemoverCampoPersonalizadoCommandHandler(_campoPersonalizadoRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverCampoPersonalizadoCommand(campo.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        _campoPersonalizadoRepository.Received(1).Remover(campo);
    }

    [Fact]
    public async Task Handle_CampoNaoEncontrado_RetornaFalha()
    {
        _campoPersonalizadoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((CampoPersonalizado?)null);
        var handler = new RemoverCampoPersonalizadoCommandHandler(_campoPersonalizadoRepository, _unitOfWork);

        var resultado = await handler.Handle(new RemoverCampoPersonalizadoCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
