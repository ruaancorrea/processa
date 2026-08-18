using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Application.TiposProcesso;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class AtualizarTipoProcessoCommandHandlerTests
{
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IVerificadorMembroEquipe _verificadorMembroEquipe = Substitute.For<IVerificadorMembroEquipe>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private AtualizarTipoProcessoCommandHandler CriarHandler() =>
        new(_tipoProcessoRepository, _verificadorMembroEquipe, _unitOfWork);

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, true, ModoAtribuicao.Manual, null).Value;

    [Fact]
    public async Task Handle_TipoProcessoNaoEncontrado_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);

        var resultado = await CriarHandler().Handle(
            new AtualizarTipoProcessoCommand(Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DadosValidos_Atualiza()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);

        var resultado = await CriarHandler().Handle(
            new AtualizarTipoProcessoCommand(tipoProcesso.Id, "Nome novo", null, true, ModoAtribuicao.Manual, null),
            CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.Nome.Should().Be("Nome novo");
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModoFixoResponsavelNaoEhMembroDaEquipe_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _verificadorMembroEquipe.EhMembroAsync(Arg.Any<Guid>(), Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new AtualizarTipoProcessoCommand(tipoProcesso.Id, "Nome", null, true, ModoAtribuicao.Fixo, Guid.NewGuid()),
            CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class AlterarStatusTipoProcessoCommandHandlerTests
{
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

    [Fact]
    public async Task Ativar_TipoProcessoExiste_Ativa()
    {
        var tipoProcesso = CriarTipoProcesso();
        tipoProcesso.Desativar();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var handler = new AtivarTipoProcessoCommandHandler(_tipoProcessoRepository, _unitOfWork);

        var resultado = await handler.Handle(new AtivarTipoProcessoCommand(tipoProcesso.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.Ativo.Should().BeTrue();
    }

    [Fact]
    public async Task Desativar_TipoProcessoExiste_Desativa()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        var handler = new DesativarTipoProcessoCommandHandler(_tipoProcessoRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarTipoProcessoCommand(tipoProcesso.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.Ativo.Should().BeFalse();
    }

    [Fact]
    public async Task Desativar_TipoProcessoNaoEncontrado_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);
        var handler = new DesativarTipoProcessoCommandHandler(_tipoProcessoRepository, _unitOfWork);

        var resultado = await handler.Handle(new DesativarTipoProcessoCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class DefinirPermissoesInicioCommandHandlerTests
{
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IVerificadorUsuario _verificadorUsuario = Substitute.For<IVerificadorUsuario>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DefinirPermissoesInicioCommandHandler CriarHandler() =>
        new(_tipoProcessoRepository, _verificadorUsuario, _unitOfWork);

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

    [Fact]
    public async Task Handle_UsuariosExistem_DefinePermissoes()
    {
        var tipoProcesso = CriarTipoProcesso();
        var usuarioId = Guid.NewGuid();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _verificadorUsuario.ExisteAsync(usuarioId).Returns(true);

        var resultado = await CriarHandler().Handle(
            new DefinirPermissoesInicioCommand(tipoProcesso.Id, [Perfil.Gestor], [usuarioId]), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.PermissoesInicio.UsuarioIds.Should().Contain(usuarioId);
    }

    [Fact]
    public async Task Handle_UsuarioNaoExiste_RetornaFalha()
    {
        var tipoProcesso = CriarTipoProcesso();
        var usuarioId = Guid.NewGuid();
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _verificadorUsuario.ExisteAsync(usuarioId).Returns(false);

        var resultado = await CriarHandler().Handle(
            new DefinirPermissoesInicioCommand(tipoProcesso.Id, [], [usuarioId]), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
        await _unitOfWork.DidNotReceive().SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PerfisEUsuarioIdsNulos_NaoLancaExcecaoELimpaPermissoes()
    {
        var tipoProcesso = CriarTipoProcesso();
        tipoProcesso.DefinirPermissoesInicio(PermissoesInicio.Criar([Perfil.Admin], []));
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);

        var resultado = await CriarHandler().Handle(
            new DefinirPermissoesInicioCommand(tipoProcesso.Id, null, null), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        tipoProcesso.PermissoesInicio.Should().Be(PermissoesInicio.Vazia);
    }

    [Fact]
    public async Task Handle_TipoProcessoNaoEncontrado_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);

        var resultado = await CriarHandler().Handle(
            new DefinirPermissoesInicioCommand(Guid.NewGuid(), [], []), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}

public class TiposProcessoQueriesTests
{
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly ICampoPersonalizadoRepository _campoPersonalizadoRepository = Substitute.For<ICampoPersonalizadoRepository>();
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();

    private static TipoProcesso CriarTipoProcesso() =>
        TipoProcesso.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, true, ModoAtribuicao.Manual, null).Value;

    [Fact]
    public async Task ListarTiposProcesso_RetornaResumoDeTodos()
    {
        var tipoProcesso = CriarTipoProcesso();
        _tipoProcessoRepository.ListarAsync().Returns([tipoProcesso]);
        var handler = new ListarTiposProcessoQueryHandler(_tipoProcessoRepository);

        var resultado = await handler.Handle(new ListarTiposProcessoQuery(), CancellationToken.None);

        resultado.Should().ContainSingle(t => t.Id == tipoProcesso.Id);
    }

    [Fact]
    public async Task ObterTipoProcessoPorId_Existe_RetornaDetalheComCamposEFluxos()
    {
        var tipoProcesso = CriarTipoProcesso();
        var campo = CampoPersonalizado.Criar(
            tipoProcesso.TenantId, tipoProcesso.Id, "Campo", TipoCampoPersonalizado.Texto, null, false, 0).Value;
        var fluxo = Fluxo.Criar(tipoProcesso.TenantId, tipoProcesso.Id, "Fluxo", null, true).Value;
        _tipoProcessoRepository.ObterPorIdAsync(tipoProcesso.Id).Returns(tipoProcesso);
        _campoPersonalizadoRepository.ListarPorTipoProcessoAsync(tipoProcesso.Id).Returns([campo]);
        _fluxoRepository.ListarPorTipoProcessoAsync(tipoProcesso.Id).Returns([fluxo]);
        var handler = new ObterTipoProcessoPorIdQueryHandler(_tipoProcessoRepository, _campoPersonalizadoRepository, _fluxoRepository);

        var resultado = await handler.Handle(new ObterTipoProcessoPorIdQuery(tipoProcesso.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Campos.Should().ContainSingle(c => c.Id == campo.Id);
        resultado.Value.Fluxos.Should().ContainSingle(f => f.Id == fluxo.Id);
    }

    [Fact]
    public async Task ObterTipoProcessoPorId_NaoEncontrado_RetornaFalha()
    {
        _tipoProcessoRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((TipoProcesso?)null);
        var handler = new ObterTipoProcessoPorIdQueryHandler(_tipoProcessoRepository, _campoPersonalizadoRepository, _fluxoRepository);

        var resultado = await handler.Handle(new ObterTipoProcessoPorIdQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }
}
