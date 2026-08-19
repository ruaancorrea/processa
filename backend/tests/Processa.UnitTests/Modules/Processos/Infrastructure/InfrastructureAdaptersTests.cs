using FluentAssertions;
using MediatR;
using NSubstitute;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Modules.Processos.Infrastructure;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Infrastructure;

public class ResolvedorValorCampoTests
{
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();

    private ResolvedorValorCampo CriarResolvedor() => new(_execucaoEtapaRepository);

    [Fact]
    public async Task ObterValorAsync_SemExecucoes_RetornaNull()
    {
        _execucaoEtapaRepository.ListarPorDemandaAsync(Arg.Any<Guid>()).Returns([]);

        var valor = await CriarResolvedor().ObterValorAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        valor.Should().BeNull();
    }

    [Fact]
    public async Task ObterValorAsync_CampoNaoPreenchidoEmNenhumaExecucao_RetornaNull()
    {
        var demandaId = Guid.NewGuid();
        var execucao = ExecucaoEtapa.Criar(Guid.NewGuid(), demandaId, Guid.NewGuid(), null).Value;
        _execucaoEtapaRepository.ListarPorDemandaAsync(demandaId).Returns([execucao]);

        var valor = await CriarResolvedor().ObterValorAsync(execucao.TenantId, demandaId, Guid.NewGuid(), CancellationToken.None);

        valor.Should().BeNull();
    }

    [Fact]
    public async Task ObterValorAsync_ValorSobrescritoEmExecucaoMaisRecente_RetornaOMaisRecente()
    {
        var demandaId = Guid.NewGuid();
        var campoId = Guid.NewGuid();
        var execucaoAntiga = ExecucaoEtapa.Criar(Guid.NewGuid(), demandaId, Guid.NewGuid(), null).Value;
        execucaoAntiga.DefinirValorCampo(campoId, "valor-antigo");
        // Sleep deliberado: CreatedAt vem de DateTimeOffset.UtcNow sem seam de relógio
        // injetável na entidade: sem isso, duas construções em sequência apertada podem
        // cair no mesmo tick e o teste da regra "mais recente vence" fica não-determinístico.
        Thread.Sleep(20);
        var execucaoRecente = ExecucaoEtapa.Criar(execucaoAntiga.TenantId, demandaId, Guid.NewGuid(), null).Value;
        execucaoRecente.DefinirValorCampo(campoId, "valor-recente");
        _execucaoEtapaRepository.ListarPorDemandaAsync(demandaId).Returns([execucaoAntiga, execucaoRecente]);

        var valor = await CriarResolvedor().ObterValorAsync(execucaoAntiga.TenantId, demandaId, campoId, CancellationToken.None);

        valor.Should().Be("valor-recente");
    }
}

public class CriadorSubprocessoTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly IDemandaRepository _demandaRepository = Substitute.For<IDemandaRepository>();

    private CriadorSubprocesso CriarCriador() => new(_sender, _demandaRepository);

    private static Demanda CriarDemandaPai(Guid? responsavelId) =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), responsavelId, Prioridade.Alta, null).Value;

    [Fact]
    public async Task CriarAsync_DemandaPaiInexistente_Lanca()
    {
        _demandaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((Demanda?)null);

        var act = () => CriarCriador().CriarAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CriarAsync_FalhaAoEnviarComando_Lanca()
    {
        var demandaPai = CriarDemandaPai(Guid.NewGuid());
        _demandaRepository.ObterPorIdAsync(demandaPai.Id).Returns(demandaPai);
        _sender.Send(Arg.Any<CriarDemandaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<Guid>("erro"));

        var act = () => CriarCriador().CriarAsync(demandaPai.TenantId, demandaPai.Id, Guid.NewGuid(), true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CriarAsync_HerdarResponsavelTrue_PropagaResponsavelClienteEPrioridadeDoPai()
    {
        var demandaPai = CriarDemandaPai(Guid.NewGuid());
        _demandaRepository.ObterPorIdAsync(demandaPai.Id).Returns(demandaPai);
        var novoId = Guid.NewGuid();
        _sender.Send(Arg.Any<CriarDemandaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(novoId));

        var resultado = await CriarCriador().CriarAsync(demandaPai.TenantId, demandaPai.Id, Guid.NewGuid(), herdarResponsavel: true, CancellationToken.None);

        resultado.Should().Be(novoId);
        await _sender.Received(1).Send(
            Arg.Is<CriarDemandaCommand>(c =>
                c.ResponsavelId == demandaPai.ResponsavelId && c.ClienteId == demandaPai.ClienteId &&
                c.Prioridade == demandaPai.Prioridade && c.DemandaPaiId == demandaPai.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CriarAsync_HerdarResponsavelFalse_NaoPropagaResponsavel()
    {
        var demandaPai = CriarDemandaPai(Guid.NewGuid());
        _demandaRepository.ObterPorIdAsync(demandaPai.Id).Returns(demandaPai);
        _sender.Send(Arg.Any<CriarDemandaCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success(Guid.NewGuid()));

        await CriarCriador().CriarAsync(demandaPai.TenantId, demandaPai.Id, Guid.NewGuid(), herdarResponsavel: false, CancellationToken.None);

        await _sender.Received(1).Send(Arg.Is<CriarDemandaCommand>(c => c.ResponsavelId == null), Arg.Any<CancellationToken>());
    }
}

public class VerificadorDesdobramentosTests
{
    private readonly IExecucaoEtapaRepository _execucaoEtapaRepository = Substitute.For<IExecucaoEtapaRepository>();
    private readonly IEtapaRepository _etapaRepository = Substitute.For<IEtapaRepository>();
    private readonly IDesdobramentoAguardadoRepository _desdobramentoRepository = Substitute.For<IDesdobramentoAguardadoRepository>();

    private VerificadorDesdobramentos CriarVerificador() => new(_execucaoEtapaRepository, _etapaRepository, _desdobramentoRepository);

    private ExecucaoEtapa PrepararUniao(params Guid[] etapasAguardadas)
    {
        var tenantId = Guid.NewGuid();
        var etapaUniao = Etapa.Criar(
            tenantId, Guid.NewGuid(), "União", null, TipoEtapa.Uniao, 1, new ConfiguracaoEtapaUniao(etapasAguardadas)).Value;
        var execucaoUniao = ExecucaoEtapa.Criar(tenantId, Guid.NewGuid(), etapaUniao.Id, null).Value;
        _execucaoEtapaRepository.ObterPorIdAsync(execucaoUniao.Id).Returns(execucaoUniao);
        _etapaRepository.ObterPorIdAsync(etapaUniao.Id).Returns(etapaUniao);
        return execucaoUniao;
    }

    [Fact]
    public async Task TodosConcluidosAsync_ExecucaoUniaoInexistente_RetornaFalse()
    {
        _execucaoEtapaRepository.ObterPorIdAsync(Arg.Any<Guid>()).Returns((ExecucaoEtapa?)null);

        var resultado = await CriarVerificador().TodosConcluidosAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task TodosConcluidosAsync_EtapaNaoEhUniao_RetornaFalse()
    {
        var tenantId = Guid.NewGuid();
        var etapaComum = Etapa.Criar(tenantId, Guid.NewGuid(), "Comum", null, TipoEtapa.Comum, 1, null).Value;
        var execucao = ExecucaoEtapa.Criar(tenantId, Guid.NewGuid(), etapaComum.Id, null).Value;
        _execucaoEtapaRepository.ObterPorIdAsync(execucao.Id).Returns(execucao);
        _etapaRepository.ObterPorIdAsync(etapaComum.Id).Returns(etapaComum);

        var resultado = await CriarVerificador().TodosConcluidosAsync(tenantId, execucao.Id, CancellationToken.None);

        resultado.Should().BeFalse();
    }

    /// <summary>A regra crítica documentada em VerificadorDesdobramentos: com só 1 dos 2 ramos concluído, ainda não pode liberar a União.</summary>
    [Fact]
    public async Task TodosConcluidosAsync_MenosDesdobramentosRegistradosQueEtapasAguardadas_RetornaFalse()
    {
        var execucaoUniao = PrepararUniao(Guid.NewGuid(), Guid.NewGuid());
        var desdobramentoUnico = DesdobramentoAguardado.Criar(execucaoUniao.TenantId, execucaoUniao.Id, Guid.NewGuid()).Value;
        desdobramentoUnico.MarcarConcluido();
        _desdobramentoRepository.ListarPorExecucaoUniaoAsync(execucaoUniao.Id).Returns([desdobramentoUnico]);

        var resultado = await CriarVerificador().TodosConcluidosAsync(execucaoUniao.TenantId, execucaoUniao.Id, CancellationToken.None);

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task TodosConcluidosAsync_TodosOsRegistradosMasNemTodosConcluidos_RetornaFalse()
    {
        var execucaoUniao = PrepararUniao(Guid.NewGuid(), Guid.NewGuid());
        var concluido = DesdobramentoAguardado.Criar(execucaoUniao.TenantId, execucaoUniao.Id, Guid.NewGuid()).Value;
        concluido.MarcarConcluido();
        var pendente = DesdobramentoAguardado.Criar(execucaoUniao.TenantId, execucaoUniao.Id, Guid.NewGuid()).Value;
        _desdobramentoRepository.ListarPorExecucaoUniaoAsync(execucaoUniao.Id).Returns([concluido, pendente]);

        var resultado = await CriarVerificador().TodosConcluidosAsync(execucaoUniao.TenantId, execucaoUniao.Id, CancellationToken.None);

        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task TodosConcluidosAsync_TodosRegistradosEConcluidos_RetornaTrue()
    {
        var execucaoUniao = PrepararUniao(Guid.NewGuid(), Guid.NewGuid());
        var primeiro = DesdobramentoAguardado.Criar(execucaoUniao.TenantId, execucaoUniao.Id, Guid.NewGuid()).Value;
        primeiro.MarcarConcluido();
        var segundo = DesdobramentoAguardado.Criar(execucaoUniao.TenantId, execucaoUniao.Id, Guid.NewGuid()).Value;
        segundo.MarcarConcluido();
        _desdobramentoRepository.ListarPorExecucaoUniaoAsync(execucaoUniao.Id).Returns([primeiro, segundo]);

        var resultado = await CriarVerificador().TodosConcluidosAsync(execucaoUniao.TenantId, execucaoUniao.Id, CancellationToken.None);

        resultado.Should().BeTrue();
    }
}

/// <summary>Mitigação de SSRF da Etapa Automatizada (.faf/pendencias.faf) — só a parte pura (classificação de IP) é testável sem abrir socket de verdade.</summary>
public class ClienteHttpEtapaConnectGuardTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.1")]
    [InlineData("169.254.169.254")] // metadata de nuvem (AWS/GCP/Azure) — o alvo clássico de SSRF
    [InlineData("0.0.0.0")]
    [InlineData("::1")]
    [InlineData("::ffff:169.254.169.254")] // IPv4-mapeado-em-IPv6: não pode escapar a checagem
    public void EnderecoEhPermitido_EnderecoPrivadoOuEspecial_RetornaFalse(string enderecoTexto)
    {
        var endereco = System.Net.IPAddress.Parse(enderecoTexto);

        ClienteHttpEtapaConnectGuard.EnderecoEhPermitido(endereco).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("1.1.1.1")]
    [InlineData("172.32.0.1")] // logo fora do bloco 172.16.0.0/12
    [InlineData("172.15.255.255")] // logo abaixo do bloco 172.16.0.0/12
    [InlineData("192.167.255.255")] // logo abaixo do bloco 192.168.0.0/16
    public void EnderecoEhPermitido_EnderecoPublico_RetornaTrue(string enderecoTexto)
    {
        var endereco = System.Net.IPAddress.Parse(enderecoTexto);

        ClienteHttpEtapaConnectGuard.EnderecoEhPermitido(endereco).Should().BeTrue();
    }
}
