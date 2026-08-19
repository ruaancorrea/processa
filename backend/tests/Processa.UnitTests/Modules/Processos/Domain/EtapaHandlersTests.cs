using System.Text.Json;
using FluentAssertions;
using NSubstitute;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class EtapaConclusaoHandlerTests
{
    [Fact]
    public async Task ExecutarAsync_SempreConcluiImediatamente()
    {
        var handler = new EtapaConclusaoHandler();
        var contexto = new ExecucaoEtapaContexto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "{}");

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
    }
}

public class EtapaCondicionalHandlerTests
{
    private readonly IResolvedorValorCampo _resolvedorValorCampo = Substitute.For<IResolvedorValorCampo>();

    private static ExecucaoEtapaContexto CriarContexto(ConfiguracaoEtapaCondicional configuracao) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao));

    [Fact]
    public async Task ExecutarAsync_UmRamoCasa_VaiParaEtapaDoRamo()
    {
        var campoId = Guid.NewGuid();
        var etapaDestino = Guid.NewGuid();
        var etapaPadrao = Guid.NewGuid();
        var configuracao = new ConfiguracaoEtapaCondicional(
            [new RamoCondicional(campoId, OperadorCondicional.Igual, "Alta", etapaDestino)], etapaPadrao);
        _resolvedorValorCampo.ObterValorAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), campoId).Returns("Alta");
        var handler = new EtapaCondicionalHandler(_resolvedorValorCampo);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.ProximasEtapasIds.Should().BeEquivalentTo([etapaDestino]);
    }

    [Fact]
    public async Task ExecutarAsync_NenhumRamoCasa_VaiParaEtapaPadrao()
    {
        var campoId = Guid.NewGuid();
        var etapaPadrao = Guid.NewGuid();
        var configuracao = new ConfiguracaoEtapaCondicional(
            [new RamoCondicional(campoId, OperadorCondicional.Igual, "Alta", Guid.NewGuid())], etapaPadrao);
        _resolvedorValorCampo.ObterValorAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), campoId).Returns("Baixa");
        var handler = new EtapaCondicionalHandler(_resolvedorValorCampo);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.ProximasEtapasIds.Should().BeEquivalentTo([etapaPadrao]);
    }

    [Fact]
    public async Task ExecutarAsync_MaisDeUmRamoCasa_ForkParaTodosOsDestinos()
    {
        var campoId = Guid.NewGuid();
        var etapaDestino1 = Guid.NewGuid();
        var etapaDestino2 = Guid.NewGuid();
        var configuracao = new ConfiguracaoEtapaCondicional(
            [
                new RamoCondicional(campoId, OperadorCondicional.MaiorQue, "0", etapaDestino1),
                new RamoCondicional(campoId, OperadorCondicional.MenorQue, "100", etapaDestino2),
            ],
            Guid.NewGuid());
        _resolvedorValorCampo.ObterValorAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), campoId).Returns("50");
        var handler = new EtapaCondicionalHandler(_resolvedorValorCampo);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.ProximasEtapasIds.Should().BeEquivalentTo([etapaDestino1, etapaDestino2]);
    }

    [Theory]
    [InlineData(OperadorCondicional.Igual, "10", "10", true)]
    [InlineData(OperadorCondicional.Diferente, "10", "20", true)]
    [InlineData(OperadorCondicional.Contem, "Processo Urgente", "Urgente", true)]
    [InlineData(OperadorCondicional.MaiorQue, "10", "5", true)]
    [InlineData(OperadorCondicional.MenorQue, "3", "5", true)]
    [InlineData(OperadorCondicional.MaiorQue, "3", "5", false)]
    public void Avalia_CasosDeOperador(OperadorCondicional operador, string valorReal, string valorEsperado, bool esperado)
    {
        EtapaCondicionalHandler.Avalia(operador, valorReal, valorEsperado).Should().Be(esperado);
    }
}

public class EtapaAutomatizadaHandlerTests
{
    private readonly IClienteHttpEtapa _clienteHttp = Substitute.For<IClienteHttpEtapa>();

    private static ExecucaoEtapaContexto CriarContexto(ConfiguracaoEtapaAutomatizada configuracao) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao));

    [Fact]
    public async Task ExecutarAsync_StatusIgualAoEsperado_Conclui()
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada("https://exemplo.com/api", MetodoHttp.Post, null, 200);
        _clienteHttp.EnviarAsync(configuracao.Url, configuracao.Metodo, configuracao.CorpoTemplate).Returns(new RespostaHttpEtapa(true, 200, null));
        var handler = new EtapaAutomatizadaHandler(_clienteHttp);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
    }

    [Fact]
    public async Task ExecutarAsync_StatusDiferenteDoEsperado_Trava()
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada("https://exemplo.com/api", MetodoHttp.Post, null, 200);
        _clienteHttp.EnviarAsync(configuracao.Url, configuracao.Metodo, configuracao.CorpoTemplate).Returns(new RespostaHttpEtapa(true, 500, null));
        var handler = new EtapaAutomatizadaHandler(_clienteHttp);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.Desfecho.Should().Be(DesfechoExecucao.Travada);
    }

    [Fact]
    public async Task ExecutarAsync_FalhaDeTransporte_Trava()
    {
        var configuracao = new ConfiguracaoEtapaAutomatizada("https://exemplo.com/api", MetodoHttp.Get, null, 200);
        _clienteHttp.EnviarAsync(configuracao.Url, configuracao.Metodo, configuracao.CorpoTemplate)
            .Returns(new RespostaHttpEtapa(false, null, "timeout"));
        var handler = new EtapaAutomatizadaHandler(_clienteHttp);

        var resultado = await handler.ExecutarAsync(CriarContexto(configuracao));

        resultado.Desfecho.Should().Be(DesfechoExecucao.Travada);
        resultado.Motivo.Should().Contain("timeout");
    }
}

public class EtapaNotificacaoHandlerTests
{
    private readonly INotificadorEtapa _notificadorEtapa = Substitute.For<INotificadorEtapa>();

    [Fact]
    public async Task ExecutarAsync_EnviaENuncaAguarda()
    {
        var configuracao = new ConfiguracaoEtapaNotificacao(DestinatarioNotificacao.Responsavel, CanalNotificacao.Email, "Prazo vencendo");
        var contexto = new ExecucaoEtapaContexto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao));
        var handler = new EtapaNotificacaoHandler(_notificadorEtapa);

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
        await _notificadorEtapa.Received(1).EnviarAsync(
            contexto.TenantId, contexto.DemandaId, configuracao.Destinatario, configuracao.Canal, configuracao.Mensagem);
    }
}

public class EtapaAgendamentoHandlerTests
{
    [Fact]
    public async Task ExecutarAsync_DataAlvoJaPassou_Conclui()
    {
        var configuracao = new ConfiguracaoEtapaAgendamento(3);
        var contexto = new ExecucaoEtapaContexto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao), DateTimeOffset.UtcNow.AddDays(-5));
        var handler = new EtapaAgendamentoHandler();

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
    }

    [Fact]
    public async Task ExecutarAsync_DataAlvoAindaNaoChegou_Aguarda()
    {
        var configuracao = new ConfiguracaoEtapaAgendamento(3);
        var contexto = new ExecucaoEtapaContexto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao), DateTimeOffset.UtcNow);
        var handler = new EtapaAgendamentoHandler();

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Aguardando);
    }
}

public class EtapaSubprocessoHandlerTests
{
    private readonly ICriadorSubprocesso _criadorSubprocesso = Substitute.For<ICriadorSubprocesso>();

    [Fact]
    public async Task ExecutarAsync_DisparaFilhoEAguarda()
    {
        var configuracao = new ConfiguracaoEtapaSubprocesso(Guid.NewGuid(), true);
        var contexto = new ExecucaoEtapaContexto(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), JsonSerializer.Serialize(configuracao));
        var demandaFilhaId = Guid.NewGuid();
        _criadorSubprocesso.CriarAsync(contexto.TenantId, contexto.DemandaId, configuracao.TipoProcessoFilhoId, true).Returns(demandaFilhaId);
        var handler = new EtapaSubprocessoHandler(_criadorSubprocesso);

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Aguardando);
        resultado.Motivo.Should().Contain(demandaFilhaId.ToString());
        resultado.DadosResultantes.Should().ContainKey(EtapaSubprocessoHandler.DemandaFilhaChave)
            .WhoseValue.Should().Be(demandaFilhaId.ToString());
    }
}

public class EtapaUniaoHandlerTests
{
    private readonly IVerificadorDesdobramentos _verificadorDesdobramentos = Substitute.For<IVerificadorDesdobramentos>();

    private static ExecucaoEtapaContexto CriarContexto() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            JsonSerializer.Serialize(new ConfiguracaoEtapaUniao([Guid.NewGuid(), Guid.NewGuid()])));

    [Fact]
    public async Task ExecutarAsync_TodosConcluidos_Conclui()
    {
        var contexto = CriarContexto();
        _verificadorDesdobramentos.TodosConcluidosAsync(contexto.TenantId, contexto.ExecucaoEtapaId).Returns(true);
        var handler = new EtapaUniaoHandler(_verificadorDesdobramentos);

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Concluida);
    }

    [Fact]
    public async Task ExecutarAsync_AindaFaltamRamos_Aguarda()
    {
        var contexto = CriarContexto();
        _verificadorDesdobramentos.TodosConcluidosAsync(contexto.TenantId, contexto.ExecucaoEtapaId).Returns(false);
        var handler = new EtapaUniaoHandler(_verificadorDesdobramentos);

        var resultado = await handler.ExecutarAsync(contexto);

        resultado.Desfecho.Should().Be(DesfechoExecucao.Aguardando);
    }
}

public class EtapaHandlerFactoryTests
{
    [Fact]
    public void ObterHandler_TipoRegistrado_RetornaHandlerCorreto()
    {
        var factory = new EtapaHandlerFactory([new EtapaComumHandler(), new EtapaConclusaoHandler()]);

        factory.ObterHandler(TipoEtapa.Comum).Should().BeOfType<EtapaComumHandler>();
    }

    [Fact]
    public void ObterHandler_TipoNaoRegistrado_Lanca()
    {
        var factory = new EtapaHandlerFactory([new EtapaComumHandler()]);

        var act = () => factory.ObterHandler(TipoEtapa.Uniao);

        act.Should().Throw<InvalidOperationException>();
    }
}
