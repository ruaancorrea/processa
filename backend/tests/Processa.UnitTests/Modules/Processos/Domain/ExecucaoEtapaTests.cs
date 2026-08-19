using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class ExecucaoEtapaTests
{
    private static ExecucaoEtapa Criar() =>
        ExecucaoEtapa.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null).Value;

    [Fact]
    public void Criar_DadosValidos_NascePendente()
    {
        var resultado = ExecucaoEtapa.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Status.Should().Be(StatusExecucaoEtapa.Pendente);
    }

    [Fact]
    public void Iniciar_ExecucaoPendente_VaiParaEmAndamento()
    {
        var execucao = Criar();

        execucao.Iniciar();

        execucao.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
    }

    [Fact]
    public void Concluir_ExecucaoEmAndamento_ConcluiERegistraConcluidoEm()
    {
        var execucao = Criar();
        execucao.Iniciar();

        var resultado = execucao.Concluir();

        resultado.IsSuccess.Should().BeTrue();
        execucao.Status.Should().Be(StatusExecucaoEtapa.Concluida);
        execucao.ConcluidoEm.Should().NotBeNull();
    }

    [Fact]
    public void Concluir_ExecucaoJaConcluida_RetornaFalha()
    {
        var execucao = Criar();
        execucao.Concluir();

        execucao.Concluir().IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Pular_ExecucaoPendente_MarcaPulada()
    {
        var execucao = Criar();

        var resultado = execucao.Pular();

        resultado.IsSuccess.Should().BeTrue();
        execucao.Status.Should().Be(StatusExecucaoEtapa.Pulada);
    }

    [Fact]
    public void Reabrir_ExecucaoConcluida_VoltaParaEmAndamento()
    {
        var execucao = Criar();
        execucao.Concluir();

        var resultado = execucao.Reabrir();

        resultado.IsSuccess.Should().BeTrue();
        execucao.Status.Should().Be(StatusExecucaoEtapa.EmAndamento);
        execucao.ConcluidoEm.Should().BeNull();
    }

    [Fact]
    public void Reabrir_ExecucaoNaoConcluida_RetornaFalha()
    {
        var execucao = Criar();

        execucao.Reabrir().IsFailure.Should().BeTrue();
    }

    [Theory]
    [InlineData(StatusExecucaoEtapa.Pendente, true)]
    [InlineData(StatusExecucaoEtapa.EmAndamento, true)]
    [InlineData(StatusExecucaoEtapa.Aguardando, true)]
    [InlineData(StatusExecucaoEtapa.Concluida, false)]
    [InlineData(StatusExecucaoEtapa.Pulada, false)]
    [InlineData(StatusExecucaoEtapa.Erro, false)]
    public void AceitaNovaInteracao_PorStatus(StatusExecucaoEtapa status, bool esperado)
    {
        var execucao = Criar();
        switch (status)
        {
            case StatusExecucaoEtapa.EmAndamento: execucao.Iniciar(); break;
            case StatusExecucaoEtapa.Aguardando: execucao.MarcarAguardando(); break;
            case StatusExecucaoEtapa.Concluida: execucao.Concluir(); break;
            case StatusExecucaoEtapa.Pulada: execucao.Pular(); break;
            case StatusExecucaoEtapa.Erro: execucao.RegistrarErro(); break;
        }

        execucao.AceitaNovaInteracao().Should().Be(esperado);
    }

    [Fact]
    public void DefinirValorCampo_ArmazenaEPreservaValoresAnteriores()
    {
        var execucao = Criar();
        var campo1 = Guid.NewGuid();
        var campo2 = Guid.NewGuid();

        execucao.DefinirValorCampo(campo1, "Alta");
        execucao.DefinirValorCampo(campo2, "Baixa");

        execucao.DadosExecucao.Should().Contain(campo1.ToString(), "Alta");
        execucao.DadosExecucao.Should().Contain(campo2.ToString(), "Baixa");
    }
}
