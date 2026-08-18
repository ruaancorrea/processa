using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class FluxoTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucesso()
    {
        var resultado = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Fluxo padrão", "Descrição", true);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.FluxoPadrao.Should().BeTrue();
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "", null, false);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_TenantOuTipoProcessoVazio_RetornaFalha()
    {
        var resultado = Fluxo.Criar(Guid.Empty, Guid.NewGuid(), "Nome", null, false);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void MarcarDesmarcarComoPadrao_AlteraFlag()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, false).Value;

        fluxo.MarcarComoPadrao();
        fluxo.FluxoPadrao.Should().BeTrue();

        fluxo.DesmarcarComoPadrao();
        fluxo.FluxoPadrao.Should().BeFalse();
    }

    [Fact]
    public void AtualizarDados_DadosValidos_Atualiza()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", null, false).Value;

        var resultado = fluxo.AtualizarDados("Nome novo", "Descrição nova");

        resultado.IsSuccess.Should().BeTrue();
        fluxo.Nome.Should().Be("Nome novo");
        fluxo.Descricao.Should().Be("Descrição nova");
    }

    [Fact]
    public void AtualizarDados_NomeVazio_RetornaFalha()
    {
        var fluxo = Fluxo.Criar(Guid.NewGuid(), Guid.NewGuid(), "Nome", null, false).Value;

        var resultado = fluxo.AtualizarDados("", null);

        resultado.IsFailure.Should().BeTrue();
    }
}
