using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class DemandaTests
{
    [Fact]
    public void Criar_SemResponsavel_NasceSemResponsavel()
    {
        var resultado = Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Prioridade.Media, null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Status.Should().Be(StatusDemanda.SemResponsavel);
    }

    [Fact]
    public void Criar_ComResponsavel_NascePendente()
    {
        var resultado = Demanda.Criar(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Prioridade.Alta, null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Status.Should().Be(StatusDemanda.Pendente);
    }

    [Fact]
    public void Criar_ClienteVazio_RetornaFalha()
    {
        var resultado = Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, Prioridade.Media, null);

        resultado.IsFailure.Should().BeTrue();
    }

    private static Demanda CriarSemResponsavel() =>
        Demanda.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, Prioridade.Media, null).Value;

    [Fact]
    public void AtribuirResponsavel_DemandaSemResponsavel_VaiParaPendente()
    {
        var demanda = CriarSemResponsavel();

        var resultado = demanda.AtribuirResponsavel(Guid.NewGuid());

        resultado.IsSuccess.Should().BeTrue();
        demanda.Status.Should().Be(StatusDemanda.Pendente);
    }

    [Fact]
    public void AtribuirResponsavel_DemandaConcluida_RetornaFalha()
    {
        var demanda = CriarSemResponsavel();
        demanda.AtribuirResponsavel(Guid.NewGuid());
        demanda.Concluir();

        var resultado = demanda.AtribuirResponsavel(Guid.NewGuid());

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void IniciarEtapa_DemandaPendente_VaiParaEmAndamento()
    {
        var demanda = CriarSemResponsavel();
        demanda.AtribuirResponsavel(Guid.NewGuid());
        var etapaId = Guid.NewGuid();

        demanda.IniciarEtapa(etapaId);

        demanda.Status.Should().Be(StatusDemanda.EmAndamento);
        demanda.EtapaAtualId.Should().Be(etapaId);
    }

    [Fact]
    public void Concluir_DemandaEmAndamento_ConcluiComPercentual100()
    {
        var demanda = CriarSemResponsavel();
        demanda.AtribuirResponsavel(Guid.NewGuid());

        var resultado = demanda.Concluir();

        resultado.IsSuccess.Should().BeTrue();
        demanda.Status.Should().Be(StatusDemanda.Concluido);
        demanda.PercentualConclusao.Should().Be(100);
        demanda.DataFimReal.Should().NotBeNull();
    }

    [Fact]
    public void Concluir_DemandaJaConcluida_RetornaFalha()
    {
        var demanda = CriarSemResponsavel();
        demanda.AtribuirResponsavel(Guid.NewGuid());
        demanda.Concluir();

        demanda.Concluir().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancelar_DemandaEmAndamento_Cancela()
    {
        var demanda = CriarSemResponsavel();

        var resultado = demanda.Cancelar();

        resultado.IsSuccess.Should().BeTrue();
        demanda.Status.Should().Be(StatusDemanda.Cancelado);
    }

    [Fact]
    public void AtualizarPercentualConclusao_ForaDoIntervalo_ClampaEntre0E100()
    {
        var demanda = CriarSemResponsavel();

        demanda.AtualizarPercentualConclusao(150);
        demanda.PercentualConclusao.Should().Be(100);

        demanda.AtualizarPercentualConclusao(-10);
        demanda.PercentualConclusao.Should().Be(0);
    }
}
