using FluentAssertions;
using Processa.Modules.Clientes.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Clientes.Domain;

public class GrupoClienteTests
{
    [Fact]
    public void Criar_DadosValidos_RetornaSucesso()
    {
        var resultado = GrupoCliente.Criar(Guid.NewGuid(), "Grupo Varejo", "Clientes do setor varejista");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nome.Should().Be("Grupo Varejo");
    }

    [Fact]
    public void Criar_NomeVazio_RetornaFalha()
    {
        var resultado = GrupoCliente.Criar(Guid.NewGuid(), "", null);

        resultado.IsFailure.Should().BeTrue();
    }
}
