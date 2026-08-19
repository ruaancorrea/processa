using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Application;

/// <summary>
/// Testa o orquestrador (a peça mais arriscada do Sprint 5: fork/join real) com
/// repositórios fake em memória — precisa de comportamento stateful de verdade
/// (Add seguido de Get), o que NSubstitute puro não dá de forma natural pra um
/// fluxo com múltiplos passos.
/// </summary>
public class OrquestradorExecucaoTests
{
    private readonly FakeEtapaRepository _etapaRepository = new();
    private readonly FakeExecucaoEtapaRepository _execucaoEtapaRepository = new();
    private readonly FakeDesdobramentoAguardadoRepository _desdobramentoRepository = new();
    private readonly FakeDemandaRepository _demandaRepository = new();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly Dictionary<TipoEtapa, IEtapaHandler> _handlers = new();

    private OrquestradorExecucao CriarOrquestrador() => new(
        _etapaRepository, _execucaoEtapaRepository, _desdobramentoRepository, _demandaRepository,
        new EtapaHandlerFactory(_handlers.Values), _unitOfWork);

    private void RegistrarHandler(TipoEtapa tipo, ResultadoExecucaoEtapa resultado)
    {
        var handler = Substitute.For<IEtapaHandler>();
        handler.Tipo.Returns(tipo);
        handler.ExecutarAsync(Arg.Any<ExecucaoEtapaContexto>(), Arg.Any<CancellationToken>()).Returns(resultado);
        _handlers[tipo] = handler;
    }

    /// <summary>
    /// Handler de União real (não mockado) — o próprio objetivo destes testes é
    /// exercitar a lógica real de fork/join, então mockar o resultado da União
    /// esconderia justamente o que se quer verificar. IVerificadorDesdobramentos
    /// fake espelha a lógica real de Infrastructure.VerificadorDesdobramentos,
    /// só lendo dos repositórios fake em memória em vez do EF.
    /// </summary>
    private void RegistrarHandlerUniaoReal()
    {
        var verificador = new FakeVerificadorDesdobramentos(_execucaoEtapaRepository, _etapaRepository, _desdobramentoRepository);
        _handlers[TipoEtapa.Uniao] = new EtapaUniaoHandler(verificador);
    }

    private static Demanda CriarDemanda(Guid fluxoId) =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), fluxoId, Guid.NewGuid(), Guid.NewGuid(), Prioridade.Media, null).Value;

    private Etapa AdicionarEtapa(Guid fluxoId, TipoEtapa tipo, int ordem, ConfiguracaoEtapa? configuracao = null)
    {
        // Etapa.Criar valida a forma da Configuracao mesmo quando o handler que a
        // interpreta de verdade está mockado (RegistrarHandler) — precisa de algo
        // minimamente válido pros tipos que exigem configuração, mesmo que o
        // conteúdo real nunca seja lido (o handler mockado ignora o JSON).
        configuracao ??= tipo switch
        {
            TipoEtapa.Condicional => new ConfiguracaoEtapaCondicional(
                [new RamoCondicional(Guid.NewGuid(), OperadorCondicional.Igual, "x", Guid.NewGuid())], Guid.NewGuid()),
            TipoEtapa.Automatizada => new ConfiguracaoEtapaAutomatizada("https://exemplo.com", MetodoHttp.Get, null, 200),
            TipoEtapa.Notificacao => new ConfiguracaoEtapaNotificacao(DestinatarioNotificacao.Responsavel, CanalNotificacao.Interno, "msg"),
            TipoEtapa.Agendamento => new ConfiguracaoEtapaAgendamento(1),
            TipoEtapa.Subprocesso => new ConfiguracaoEtapaSubprocesso(Guid.NewGuid(), true),
            TipoEtapa.Uniao => new ConfiguracaoEtapaUniao([Guid.NewGuid(), Guid.NewGuid()]),
            _ => null,
        };

        var etapa = Etapa.Criar(Guid.NewGuid(), fluxoId, $"Etapa {ordem}", null, tipo, ordem, configuracao).Value;
        _etapaRepository.Etapas.Add(etapa);
        return etapa;
    }

    [Fact]
    public async Task IniciarAsync_PrimeiraEtapaComum_FicaAguardandoAcaoHumana()
    {
        var fluxoId = Guid.NewGuid();
        var etapaComum = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 0);
        var demanda = CriarDemanda(fluxoId);

        await CriarOrquestrador().IniciarAsync(demanda, CancellationToken.None);

        var execucao = await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaComum.Id);
        execucao!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
        demanda.Status.Should().Be(StatusDemanda.EmAndamento);
    }

    [Fact]
    public async Task IniciarAsync_EtapaAutomatizadaConclui_AvancaSozinhoParaProximaLinear()
    {
        var fluxoId = Guid.NewGuid();
        AdicionarEtapa(fluxoId, TipoEtapa.Automatizada, 0);
        var etapaComum = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 1);
        RegistrarHandler(TipoEtapa.Automatizada, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida));
        var demanda = CriarDemanda(fluxoId);

        await CriarOrquestrador().IniciarAsync(demanda, CancellationToken.None);

        var execucaoComum = await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaComum.Id);
        execucaoComum.Should().NotBeNull();
        execucaoComum!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
    }

    [Fact]
    public async Task IniciarAsync_UltimaEtapaConclusao_ConcluiADemanda()
    {
        var fluxoId = Guid.NewGuid();
        AdicionarEtapa(fluxoId, TipoEtapa.Conclusao, 0);
        RegistrarHandler(TipoEtapa.Conclusao, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida));
        var demanda = CriarDemanda(fluxoId);

        await CriarOrquestrador().IniciarAsync(demanda, CancellationToken.None);

        demanda.Status.Should().Be(StatusDemanda.Concluido);
        demanda.PercentualConclusao.Should().Be(100);
    }

    [Fact]
    public async Task IniciarAsync_CondicionalComUmDestino_RedirecionaSemForkar()
    {
        var fluxoId = Guid.NewGuid();
        var etapaComumIntermediaria = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 1);
        AdicionarEtapa(fluxoId, TipoEtapa.Condicional, 0);
        var etapaDestino = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 2);
        RegistrarHandler(TipoEtapa.Condicional, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, [etapaDestino.Id]));
        var demanda = CriarDemanda(fluxoId);

        await CriarOrquestrador().IniciarAsync(demanda, CancellationToken.None);

        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaDestino.Id)).Should().NotBeNull();
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaComumIntermediaria.Id)).Should().BeNull();
    }

    [Fact]
    public async Task IniciarAsync_CondicionalComDoisDestinos_ForkaOsDoisRamosEmParalelo()
    {
        var fluxoId = Guid.NewGuid();
        AdicionarEtapa(fluxoId, TipoEtapa.Condicional, 0);
        var ramo1 = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 1);
        var ramo2 = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 2);
        RegistrarHandler(TipoEtapa.Condicional, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, [ramo1.Id, ramo2.Id]));
        var demanda = CriarDemanda(fluxoId);

        await CriarOrquestrador().IniciarAsync(demanda, CancellationToken.None);

        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo1.Id))!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo2.Id))!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
    }

    [Fact]
    public async Task ForkComUniao_SoAvancaQuandoOsDoisRamosConcluem()
    {
        var fluxoId = Guid.NewGuid();
        AdicionarEtapa(fluxoId, TipoEtapa.Condicional, 0);
        var ramo1 = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 1);
        var ramo2 = AdicionarEtapa(fluxoId, TipoEtapa.Comum, 2);
        var etapaUniao = AdicionarEtapa(fluxoId, TipoEtapa.Uniao, 3, new ConfiguracaoEtapaUniao([ramo1.Id, ramo2.Id]));
        var etapaFinal = AdicionarEtapa(fluxoId, TipoEtapa.Conclusao, 4);
        RegistrarHandler(TipoEtapa.Condicional, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, [ramo1.Id, ramo2.Id]));
        RegistrarHandlerUniaoReal();
        RegistrarHandler(TipoEtapa.Conclusao, new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida));
        var demanda = CriarDemanda(fluxoId);
        var orquestrador = CriarOrquestrador();

        await orquestrador.IniciarAsync(demanda, CancellationToken.None);

        // os dois ramos (Comum) ficam esperando ação humana.
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo1.Id))!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo2.Id))!.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);

        // conclui o ramo 1 manualmente — a União já é criada (registra o desdobramento
        // deste ramo) mas fica Aguardando: falta o ramo 2.
        var execucaoRamo1 = (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo1.Id))!;
        execucaoRamo1.Concluir();
        await orquestrador.ContinuarAposConclusaoManualAsync(demanda, execucaoRamo1, CancellationToken.None);

        var execucaoUniaoParcial = await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaUniao.Id);
        execucaoUniaoParcial!.Status.Should().Be(StatusExecucaoEtapa.Aguardando);
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaFinal.Id)).Should().BeNull();

        // conclui o ramo 2 — agora sim a União (e a Conclusão depois dela) devem avançar.
        var execucaoRamo2 = (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, ramo2.Id))!;
        execucaoRamo2.Concluir();
        await orquestrador.ContinuarAposConclusaoManualAsync(demanda, execucaoRamo2, CancellationToken.None);

        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaUniao.Id))!.Status.Should().Be(StatusExecucaoEtapa.Concluida);
        (await _execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaFinal.Id)).Should().NotBeNull();
        demanda.Status.Should().Be(StatusDemanda.Concluido);
    }

    private sealed class FakeEtapaRepository : IEtapaRepository
    {
        public List<Etapa> Etapas { get; } = [];

        public Task AddAsync(Etapa etapa, CancellationToken ct = default)
        {
            Etapas.Add(etapa);
            return Task.CompletedTask;
        }

        public Task<Etapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(Etapas.FirstOrDefault(e => e.Id == id));

        public Task<List<Etapa>> ListarPorFluxoAsync(Guid fluxoId, CancellationToken ct = default) =>
            Task.FromResult(Etapas.Where(e => e.FluxoId == fluxoId).ToList());

        public void Remover(Etapa etapa) => Etapas.Remove(etapa);
    }

    private sealed class FakeExecucaoEtapaRepository : IExecucaoEtapaRepository
    {
        private readonly List<ExecucaoEtapa> _execucoes = [];

        public Task AddAsync(ExecucaoEtapa execucao, CancellationToken ct = default)
        {
            _execucoes.Add(execucao);
            return Task.CompletedTask;
        }

        public Task<ExecucaoEtapa?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_execucoes.FirstOrDefault(e => e.Id == id));

        public Task<List<ExecucaoEtapa>> ListarPorDemandaAsync(Guid demandaId, CancellationToken ct = default) =>
            Task.FromResult(_execucoes.Where(e => e.DemandaId == demandaId).ToList());

        public Task<ExecucaoEtapa?> ObterPorDemandaEEtapaAsync(Guid demandaId, Guid etapaId, CancellationToken ct = default) =>
            Task.FromResult(_execucoes.FirstOrDefault(e => e.DemandaId == demandaId && e.EtapaId == etapaId));
    }

    private sealed class FakeDesdobramentoAguardadoRepository : IDesdobramentoAguardadoRepository
    {
        private readonly List<DesdobramentoAguardado> _desdobramentos = [];

        public Task AddAsync(DesdobramentoAguardado desdobramento, CancellationToken ct = default)
        {
            _desdobramentos.Add(desdobramento);
            return Task.CompletedTask;
        }

        public Task<List<DesdobramentoAguardado>> ListarPorExecucaoUniaoAsync(Guid execucaoEtapaUniaoId, CancellationToken ct = default) =>
            Task.FromResult(_desdobramentos.Where(d => d.ExecucaoEtapaUniaoId == execucaoEtapaUniaoId).ToList());

        public Task<DesdobramentoAguardado?> ObterPorExecucaoCondicionalAsync(Guid execucaoEtapaCondicionalId, CancellationToken ct = default) =>
            Task.FromResult(_desdobramentos.FirstOrDefault(d => d.ExecucaoEtapaCondicionalId == execucaoEtapaCondicionalId));
    }

    private sealed class FakeDemandaRepository : IDemandaRepository
    {
        private readonly List<Demanda> _demandas = [];

        public Task AddAsync(Demanda demanda, CancellationToken ct = default)
        {
            _demandas.Add(demanda);
            return Task.CompletedTask;
        }

        public Task<Demanda?> ObterPorIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(_demandas.FirstOrDefault(d => d.Id == id));

        public Task<List<Demanda>> ListarAsync(CancellationToken ct = default) => Task.FromResult(_demandas.ToList());

        public Task<int> ContarAtivasPorResponsavelAsync(Guid responsavelId, CancellationToken ct = default) =>
            Task.FromResult(_demandas.Count(d => d.ResponsavelId == responsavelId
                && d.Status != StatusDemanda.Concluido && d.Status != StatusDemanda.Cancelado));
    }

    /// <summary>Espelha Infrastructure.VerificadorDesdobramentos (mesma regra: nº de desdobramentos precisa bater com o configurado, não só "os que existem estão concluídos").</summary>
    private sealed class FakeVerificadorDesdobramentos(
        FakeExecucaoEtapaRepository execucaoEtapaRepository, FakeEtapaRepository etapaRepository, FakeDesdobramentoAguardadoRepository desdobramentoRepository)
        : IVerificadorDesdobramentos
    {
        public async Task<bool> TodosConcluidosAsync(Guid tenantId, Guid execucaoEtapaUniaoId, CancellationToken cancellationToken = default)
        {
            var execucaoUniao = await execucaoEtapaRepository.ObterPorIdAsync(execucaoEtapaUniaoId, cancellationToken);
            if (execucaoUniao is null)
                return false;

            var etapaUniao = await etapaRepository.ObterPorIdAsync(execucaoUniao.EtapaId, cancellationToken);
            if (etapaUniao?.Configuracao is not ConfiguracaoEtapaUniao configuracao)
                return false;

            var desdobramentos = await desdobramentoRepository.ListarPorExecucaoUniaoAsync(execucaoEtapaUniaoId, cancellationToken);
            return desdobramentos.Count >= configuracao.EtapasAguardadasIds.Count && desdobramentos.All(d => d.Concluido);
        }
    }
}
