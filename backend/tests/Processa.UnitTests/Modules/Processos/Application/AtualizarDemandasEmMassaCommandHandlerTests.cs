using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class AtualizarDemandasEmMassaCommandHandlerTests
{
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly ITipoProcessoRepository _tipoProcessoRepository = Substitute.For<ITipoProcessoRepository>();
    private readonly IVerificadorUsuario _verificadorUsuario = Substitute.For<IVerificadorUsuario>();
    private readonly IKanbanNotificador _kanbanNotificador = Substitute.For<IKanbanNotificador>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public AtualizarDemandasEmMassaCommandHandlerTests() =>
        _tipoProcessoRepository.ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([]);

    private AtualizarDemandasEmMassaCommandHandler CriarHandler() =>
        new(_demandaRepository, _tipoProcessoRepository, _verificadorUsuario, _kanbanNotificador, _unitOfWork);

    private static Demanda CriarDemanda() =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;

    [Fact]
    public async Task Handle_NovoResponsavelInexistente_RetornaFalha()
    {
        _verificadorUsuario.ExisteAsync(Arg.Any<Guid>()).Returns(false);

        var resultado = await CriarHandler().Handle(
            new AtualizarDemandasEmMassaCommand([Guid.NewGuid()], Guid.NewGuid(), null, false), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DemandaNaoEncontrada_ContaComoFalhaSemTravarOResto()
    {
        var demandaValida = CriarDemanda();
        var idInexistente = Guid.NewGuid();
        _demandaRepository.ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([demandaValida]);

        var resultado = await CriarHandler().Handle(
            new AtualizarDemandasEmMassaCommand([demandaValida.Id, idInexistente], null, Prioridade.Urgente, false), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.TotalSucesso.Should().Be(1);
        resultado.Value.Falhas.Should().ContainSingle(f => f.DemandaId == idInexistente);
        demandaValida.Prioridade.Should().Be(Prioridade.Urgente);
    }

    [Fact]
    public async Task Handle_MelhorEsforco_UmaFalhaNaoImpedeAsOutras()
    {
        var demandaAberta = CriarDemanda();
        var demandaJaCancelada = CriarDemanda();
        demandaJaCancelada.Cancelar();
        _demandaRepository.ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([demandaAberta, demandaJaCancelada]);

        var resultado = await CriarHandler().Handle(
            new AtualizarDemandasEmMassaCommand([demandaAberta.Id, demandaJaCancelada.Id], null, null, true), CancellationToken.None);

        resultado.Value.TotalSucesso.Should().Be(1);
        resultado.Value.Falhas.Should().ContainSingle(f => f.DemandaId == demandaJaCancelada.Id);
        demandaAberta.Status.Should().Be(StatusDemanda.Cancelado);
    }

    [Fact]
    public async Task Handle_AplicaResponsavelPrioridadeECancelamentoJuntos()
    {
        var demanda = CriarDemanda();
        var novoResponsavelId = Guid.NewGuid();
        _demandaRepository.ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([demanda]);
        _verificadorUsuario.ExisteAsync(novoResponsavelId).Returns(true);

        var resultado = await CriarHandler().Handle(
            new AtualizarDemandasEmMassaCommand([demanda.Id], novoResponsavelId, Prioridade.Baixa, true), CancellationToken.None);

        resultado.Value.TotalSucesso.Should().Be(1);
        demanda.ResponsavelId.Should().Be(novoResponsavelId);
        demanda.Prioridade.Should().Be(Prioridade.Baixa);
        demanda.Status.Should().Be(StatusDemanda.Cancelado);
        await _unitOfWork.Received(1).SalvarAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NenhumaDemandaAlterada_NaoNotificaKanban()
    {
        var idInexistente = Guid.NewGuid();
        _demandaRepository.ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([]);

        await CriarHandler().Handle(new AtualizarDemandasEmMassaCommand([idInexistente], null, Prioridade.Alta, false), CancellationToken.None);

        await _tipoProcessoRepository.DidNotReceive().ListarPorIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>());
    }
}
