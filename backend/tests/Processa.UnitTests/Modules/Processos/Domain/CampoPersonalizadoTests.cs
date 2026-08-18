using FluentAssertions;
using Processa.Modules.Processos.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Processos.Domain;

public class CampoPersonalizadoTests
{
    [Fact]
    public void Criar_TipoTextoSemOpcoes_RetornaSucesso()
    {
        var resultado = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Observações", TipoCampoPersonalizado.Texto, null, false, 0);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Opcoes.Should().BeEmpty();
    }

    [Fact]
    public void Criar_TipoListaSemOpcoes_RetornaFalha()
    {
        var resultado = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Prioridade", TipoCampoPersonalizado.Lista, null, false, 0);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_TipoListaComOpcoes_RetornaSucesso()
    {
        var resultado = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Prioridade", TipoCampoPersonalizado.Lista, ["Baixa", "Alta"], false, 0);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Opcoes.Should().BeEquivalentTo(["Baixa", "Alta"]);
    }

    [Fact]
    public void Criar_TipoNaoListaComOpcoes_RetornaFalha()
    {
        var resultado = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Observações", TipoCampoPersonalizado.Texto, ["a"], false, 0);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "", TipoCampoPersonalizado.Texto, null, false, 0);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AtualizarDados_DadosValidos_Atualiza()
    {
        var campo = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Nome antigo", TipoCampoPersonalizado.Numero, null, false, 0).Value;

        var resultado = campo.AtualizarDados("Nome novo", null, true, 2);

        resultado.IsSuccess.Should().BeTrue();
        campo.Nome.Should().Be("Nome novo");
        campo.Obrigatorio.Should().BeTrue();
        campo.Ordem.Should().Be(2);
    }

    [Fact]
    public void AtualizarDados_TipoListaSemOpcoes_RetornaFalha()
    {
        var campo = CampoPersonalizado.Criar(
            Guid.NewGuid(), Guid.NewGuid(), "Prioridade", TipoCampoPersonalizado.Lista, ["Baixa"], false, 0).Value;

        var resultado = campo.AtualizarDados("Prioridade", null, false, 0);

        resultado.IsFailure.Should().BeTrue();
    }
}
