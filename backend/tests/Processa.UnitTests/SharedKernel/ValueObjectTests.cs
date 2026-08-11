using FluentAssertions;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.SharedKernel;

public class ValueObjectTests
{
    private sealed class Dinheiro(decimal valor, string moeda) : ValueObject
    {
        public decimal Valor { get; } = valor;
        public string Moeda { get; } = moeda;

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Valor;
            yield return Moeda;
        }
    }

    [Fact]
    public void Igualdade_MesmosComponentes_SaoIguais()
    {
        var a = new Dinheiro(100m, "BRL");
        var b = new Dinheiro(100m, "BRL");

        (a == b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Igualdade_ComponenteDiferente_NaoSaoIguais()
    {
        var a = new Dinheiro(100m, "BRL");
        var b = new Dinheiro(100m, "USD");

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Igualdade_ComparadoComNull_NaoEIgual()
    {
        var a = new Dinheiro(1m, "BRL");

        a.Equals(null).Should().BeFalse();
    }
}
