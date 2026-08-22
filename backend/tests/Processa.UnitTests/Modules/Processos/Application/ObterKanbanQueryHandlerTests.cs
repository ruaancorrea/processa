using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

public class ObterKanbanQueryHandlerTests
{
    private readonly IFluxoRepository _fluxoRepository = Substitute.For<IFluxoRepository>();
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();
    private readonly IConsultaCliente _consultaCliente = Substitute.For<IConsultaCliente>();
    private readonly IConsultaUsuario _consultaUsuario = Substitute.For<IConsultaUsuario>();

    public ObterKanbanQueryHandlerTests()
    {
        _consultaCliente.ObterRazoesSociaisAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new Dictionary<Guid, string>());
        _consultaUsuario.ObterNomesAsync(Arg.Any<IEnumerable<Guid>>()).Returns(new Dictionary<Guid, string>());
        _execucaoEtapaRepository.ListarPorDemandaIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([]);
    }

    private ObterKanbanQueryHandler CriarHandler() =>
        new(_fluxoRepository, _etapaRepository, _demandaRepository, _execucaoEtapaRepository, _consultaCliente, _consultaUsuario);

    private static Etapa CriarEtapaComum(Guid fluxoId, string nome, int ordem) =>
        Etapa.Criar(Guid.NewGuid(), fluxoId, nome, null, TipoEtapa.Comum, ordem, null).Value;

    [Fact]
    public async Task Handle_SemFluxoPadrao_RetornaFalha()
    {
        _fluxoRepository.ObterPadraoAsync(Arg.Any<Guid>()).Returns((Fluxo?)null);

        var resultado = await CriarHandler().Handle(new ObterKanbanQuery(Guid.NewGuid()), CancellationToken.None);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_MontaColunasNaOrdemDasEtapas()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxo = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Fluxo", null, true).Value;
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxo);
        var primeira = CriarEtapaComum(fluxo.Id, "A Fazer", 0);
        var segunda = CriarEtapaComum(fluxo.Id, "Em Andamento", 1);
        _etapaRepository.ListarPorFluxoAsync(fluxo.Id).Returns([primeira, segunda]);
        _demandaRepository.ListarParaKanbanAsync(tipoProcessoId).Returns([]);

        var resultado = await CriarHandler().Handle(new ObterKanbanQuery(tipoProcessoId), CancellationToken.None);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Colunas.Should().HaveCount(2);
        resultado.Value.Colunas.Select(c => c.EtapaNome).Should().Equal("A Fazer", "Em Andamento");
    }

    [Fact]
    public async Task Handle_ExcluiCardsCujaEtapaAtualNaoPertenceAoFluxoPadrao()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxo = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Fluxo", null, true).Value;
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxo);
        var etapaDoFluxo = CriarEtapaComum(fluxo.Id, "A Fazer", 0);
        _etapaRepository.ListarPorFluxoAsync(fluxo.Id).Returns([etapaDoFluxo]);

        var demandaValida = Demanda.Criar(fluxo.TenantId, tipoProcessoId, fluxo.Id, Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;
        demandaValida.IniciarEtapa(etapaDoFluxo.Id);
        var demandaDeFluxoAntigo = Demanda.Criar(fluxo.TenantId, tipoProcessoId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;
        demandaDeFluxoAntigo.IniciarEtapa(Guid.NewGuid());
        _demandaRepository.ListarParaKanbanAsync(tipoProcessoId).Returns([demandaValida, demandaDeFluxoAntigo]);
        var execucaoValida = ExecucaoEtapa.Criar(fluxo.TenantId, demandaValida.Id, etapaDoFluxo.Id, null).Value;
        _execucaoEtapaRepository.ListarPorDemandaIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([execucaoValida]);

        var resultado = await CriarHandler().Handle(new ObterKanbanQuery(tipoProcessoId), CancellationToken.None);

        resultado.Value.Cards.Should().ContainSingle(c => c.DemandaId == demandaValida.Id);
    }

    [Fact]
    public async Task Handle_EnriqueceCardsComNomesDeClienteEResponsavel()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxo = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Fluxo", null, true).Value;
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxo);
        var etapa = CriarEtapaComum(fluxo.Id, "A Fazer", 0);
        _etapaRepository.ListarPorFluxoAsync(fluxo.Id).Returns([etapa]);

        var responsavelId = Guid.NewGuid();
        var demanda = Demanda.Criar(fluxo.TenantId, tipoProcessoId, fluxo.Id, Guid.NewGuid(), responsavelId, Prioridade.Alta, null).Value;
        demanda.IniciarEtapa(etapa.Id);
        _demandaRepository.ListarParaKanbanAsync(tipoProcessoId).Returns([demanda]);
        var execucao = ExecucaoEtapa.Criar(fluxo.TenantId, demanda.Id, etapa.Id, responsavelId).Value;
        _execucaoEtapaRepository.ListarPorDemandaIdsAsync(Arg.Any<IEnumerable<Guid>>()).Returns([execucao]);
        _consultaCliente.ObterRazoesSociaisAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns(new Dictionary<Guid, string> { [demanda.ClienteId] = "Cliente X" });
        _consultaUsuario.ObterNomesAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns(new Dictionary<Guid, string> { [responsavelId] = "Fulano" });

        var resultado = await CriarHandler().Handle(new ObterKanbanQuery(tipoProcessoId), CancellationToken.None);

        var card = resultado.Value.Cards.Should().ContainSingle().Subject;
        card.ClienteNome.Should().Be("Cliente X");
        card.ResponsavelNome.Should().Be("Fulano");
        card.ExecucaoEtapaId.Should().Be(execucao.Id);
    }

    [Fact]
    public async Task Handle_DemandaSemExecucaoCorrespondente_NaoAparaceComoCard()
    {
        var tipoProcessoId = Guid.NewGuid();
        var fluxo = Fluxo.Criar(Guid.NewGuid(), tipoProcessoId, "Fluxo", null, true).Value;
        _fluxoRepository.ObterPadraoAsync(tipoProcessoId).Returns(fluxo);
        var etapa = CriarEtapaComum(fluxo.Id, "A Fazer", 0);
        _etapaRepository.ListarPorFluxoAsync(fluxo.Id).Returns([etapa]);
        var demanda = Demanda.Criar(fluxo.TenantId, tipoProcessoId, fluxo.Id, Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;
        demanda.IniciarEtapa(etapa.Id);
        _demandaRepository.ListarParaKanbanAsync(tipoProcessoId).Returns([demanda]);
        // Sem stub de ListarPorDemandaIdsAsync específico — usa o default do construtor ([]).

        var resultado = await CriarHandler().Handle(new ObterKanbanQuery(tipoProcessoId), CancellationToken.None);

        resultado.Value.Cards.Should().BeEmpty();
    }
}
