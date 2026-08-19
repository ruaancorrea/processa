using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class DesdobramentoAguardadoTests
{
    [Fact]
    public void Criar_DadosValidos_NascePendente()
    {
        var resultado = DesdobramentoAguardado.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Concluido.Should().BeFalse();
    }

    [Fact]
    public void Criar_IdsVazios_RetornaFalha()
    {
        DesdobramentoAguardado.Criar(Guid.NewGuid(), Guid.Empty, Guid.NewGuid()).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarcarConcluido_MudaFlag()
    {
        var desdobramento = DesdobramentoAguardado.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()).Value;

        desdobramento.MarcarConcluido();

        desdobramento.Concluido.Should().BeTrue();
    }
}

public class HistoricoExecucaoEtapaTests
{
    [Fact]
    public void Registrar_DadosValidos_RetornaSucesso()
    {
        var resultado = HistoricoExecucaoEtapa.Registrar(
            Guid.NewGuid(), Guid.NewGuid(), TipoEventoHistorico.StatusAlterado, "{\"de\":\"Pendente\"}", Guid.NewGuid());

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Registrar_SemDados_UsaObjetoVazio()
    {
        var resultado = HistoricoExecucaoEtapa.Registrar(
            Guid.NewGuid(), Guid.NewGuid(), TipoEventoHistorico.ApiExecutada, null, null);

        resultado.Value.Dados.Should().Be("{}");
        resultado.Value.UsuarioId.Should().BeNull();
    }

    [Fact]
    public void Registrar_ExecucaoVazia_RetornaFalha()
    {
        HistoricoExecucaoEtapa.Registrar(Guid.NewGuid(), Guid.Empty, TipoEventoHistorico.EtapaReaberta, null, null)
            .IsFailure.Should().BeTrue();
    }
}

public class ComentarioExecucaoTests
{
    [Fact]
    public void Criar_TextoValido_RetornaSucesso()
    {
        var resultado = ComentarioExecucao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Aguardando documento do cliente.");

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Criar_TextoVazio_RetornaFalha()
    {
        ComentarioExecucao.Criar(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "").IsFailure.Should().BeTrue();
    }
}

public class AnexoExecucaoTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucesso()
    {
        var resultado = AnexoExecucao.Criar(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "contrato.pdf", Guid.NewGuid().ToString(), "anexos/abc.pdf", 2048, "application/pdf");

        resultado.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Criar_TamanhoZeroOuNegativo_RetornaFalha()
    {
        AnexoExecucao.Criar(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "contrato.pdf", Guid.NewGuid().ToString(), "anexos/abc.pdf", 0, "application/pdf")
            .IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_MimeTypeAusente_UsaOctetStream()
    {
        var resultado = AnexoExecucao.Criar(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "arquivo", Guid.NewGuid().ToString(), "anexos/x", 10, null);

        resultado.Value.MimeType.Should().Be("application/octet-stream");
    }
}
