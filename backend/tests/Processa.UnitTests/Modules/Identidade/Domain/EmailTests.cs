using FluentAssertions;
using Processa.Modules.Identidade.Domain;
using Xunit;

namespace Processa.UnitTests.Modules.Identidade.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("pessoa@exemplo.com")]
    [InlineData("Pessoa.Nome+tag@Exemplo.COM.BR")]
    public void Criar_EmailValido_NormalizaParaMinusculas(string valor)
    {
        var resultado = Email.Criar(valor);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Valor.Should().Be(valor.Trim().ToLowerInvariant());
    }

    [Theory]
    [InlineData("sem-arroba.com")]
    [InlineData("sem-dominio@")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Criar_EmailInvalido_RetornaFalha(string? valor)
    {
        var resultado = Email.Criar(valor);

        resultado.IsFailure.Should().BeTrue();
    }
}
