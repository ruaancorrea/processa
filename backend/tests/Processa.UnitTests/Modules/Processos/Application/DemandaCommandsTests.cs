using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class DemandaCommandsTests
{
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IKanbanNotificador _kanbanNotificador = Substitute.For<IKanbanNotificador>();
    private readonly IVerificadorUsuario _verificadorUsuario = Substitute.For<IVerificadorUsuario>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private static Demanda CriarDemanda() =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;

    // -------- AtualizarPrioridadeDemandaCommand --------

    private AtualizarPrioridadeDemandaCommandHandler CriarAtualizarPrioridadeHandler() => new(_demandaRepository, _unitOfWork);

    [Fact]
    public async Task AtualizarPrioridade_DemandaInexistente_RetornaFalha()
    {
        _demandaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Demanda?)null);

        var resultado = await CriarAtualizarPrioridadeHandler().Handle(
            new AtualizarPrioridadeDemandaCommand(Guid.NewGuid(), Prioridade.Alta), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AtualizarPrioridade_CasoValido_AlteraEPersiste()
    {
        var demanda = CriarDemanda();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);

        var resultado = await CriarAtualizarPrioridadeHandler().Handle(
            new AtualizarPrioridadeDemandaCommand(demanda.Id, Prioridade.Urgente), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        demanda.Prioridade.Should().Be(Prioridade.Urgente);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    // -------- CancelarDemandaCommand --------

    private CancelarDemandaCommandHandler CriarCancelarHandler() => new(_demandaRepository, _unitOfWork);

    [Fact]
    public async Task Cancelar_DemandaInexistente_RetornaFalha()
    {
        _demandaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Demanda?)null);

        var resultado = await CriarCancelarHandler().Handle(new CancelarDemandaCommand(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Cancelar_DemandaJaConcluida_RetornaFalha()
    {
        var demanda = CriarDemanda();
        demanda.Concluir();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);

        var resultado = await CriarCancelarHandler().Handle(new CancelarDemandaCommand(demanda.Id), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Cancelar_CasoValido_AlteraStatusEPersiste()
    {
        var demanda = CriarDemanda();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);

        var resultado = await CriarCancelarHandler().Handle(new CancelarDemandaCommand(demanda.Id), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        demanda.Status.Should().Be(StatusDemanda.Cancelado);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    // -------- AtribuirResponsavelDemandaCommand --------

    private AtribuirResponsavelDemandaCommandHandler CriarAtribuirResponsavelHandler() =>
        new(_demandaRepository, _tipoProcessoRepository, _verificadorUsuario, _kanbanNotificador, _unitOfWork);

    [Fact]
    public async Task AtribuirResponsavel_DemandaInexistente_RetornaFalha()
    {
        _demandaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Demanda?)null);
        _verificadorUsuario.ExisteAsync(Arg.Any<Guid>()).Returns(true);

        var resultado = await CriarAtribuirResponsavelHandler().Handle(
            new AtribuirResponsavelDemandaCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AtribuirResponsavel_UsuarioInexistente_RetornaFalha()
    {
        var demanda = CriarDemanda();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);
        _verificadorUsuario.ExisteAsync(Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarAtribuirResponsavelHandler().Handle(
            new AtribuirResponsavelDemandaCommand(demanda.Id, Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AtribuirResponsavel_DemandaCancelada_RetornaFalha()
    {
        var demanda = CriarDemanda();
        demanda.Cancelar();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);
        _verificadorUsuario.ExisteAsync(Arg.Any<Guid>()).Returns(true);

        var resultado = await CriarAtribuirResponsavelHandler().Handle(
            new AtribuirResponsavelDemandaCommand(demanda.Id, Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task AtribuirResponsavel_DemandaSemResponsavel_PassaParaPendente()
    {
        var demanda = Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Prioridade.Media, null).Value;
        var novoResponsavelId = Guid.NewGuid();
        _demandaRepository.ObterPorIdAsync(demanda.Id).Returns(demanda);
        _verificadorUsuario.ExisteAsync(novoResponsavelId).Returns(true);

        var resultado = await CriarAtribuirResponsavelHandler().Handle(
            new AtribuirResponsavelDemandaCommand(demanda.Id, novoResponsavelId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        demanda.ResponsavelId.Should().Be(novoResponsavelId);
        demanda.Status.Should().Be(StatusDemanda.Pendente);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }
}
