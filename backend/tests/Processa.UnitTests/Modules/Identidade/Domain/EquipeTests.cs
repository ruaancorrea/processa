using FluentAssertions;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Domain;

public class EquipeTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucessoEAtiva()
    {
        var resultado = Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", "Cuida da apuração de impostos");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Ativa.Should().BeTrue();
        resultado.Value.Nome.Should().Be("Equipe Fiscal");
    }

    [Fact]
    public void Criar_TenantVazio_RetornaFalha()
    {
        var resultado = Equipe.Criar(Guid.Empty, "Equipe Fiscal", null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = Equipe.Criar(Guid.NewGuid(), "", null);

        resultado.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AtualizarDados_NomeValido_Atualiza()
    {
        var equipe = Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", null).Value;

        var resultado = equipe.AtualizarDados("Equipe Tributária", "Nova descrição");

        resultado.IsSuccess.Should().BeTrue();
        equipe.Nome.Should().Be("Equipe Tributária");
        equipe.Descricao.Should().Be("Nova descrição");
    }

    [Fact]
    public void DesativarEReativar_AlternaEstado()
    {
        var equipe = Equipe.Criar(Guid.NewGuid(), "Equipe Fiscal", null).Value;

        equipe.Desativar();
        equipe.Ativa.Should().BeFalse();

        equipe.Reativar();
        equipe.Ativa.Should().BeTrue();
    }
}
