using FluentAssertions;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Domain;

public class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void Criar_CnpjValido_RetornaSucesso(string valor)
    {
        var resultado = Cnpj.Criar(valor);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Numero.Should().Be("11222333000181");
    }

    [Theory]
    [InlineData("11.222.333/0001-80")] // dígito verificador errado
    [InlineData("11111111111111")] // todos os dígitos iguais
    [InlineData("123")] // tamanho errado
    [InlineData("")]
    [InlineData(null)]
    public void Criar_CnpjInvalido_RetornaFalha(string? valor)
    {
        var resultado = Cnpj.Criar(valor);

        resultado.IsFailure.Should().BeTrue();
    }
}
