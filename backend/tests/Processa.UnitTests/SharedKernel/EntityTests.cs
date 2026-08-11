using FluentAssertions;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.SharedKernel;

public class EntityTests
{
    private sealed class ClienteFake(Guid id) : Entity(id);
    private sealed class OutraEntidadeFake(Guid id) : Entity(id);

    [Fact]
    public void Construir_ComIdVazio_Lanca()
    {
        var act = () => new ClienteFake(Guid.Empty);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Igualdade_MesmoTipoEId_SaoIguais()
    {
        var id = Guid.NewGuid();
        var a = new ClienteFake(id);
        var b = new ClienteFake(id);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Igualdade_TiposDiferentes_MesmoId_NaoSaoIguais()
    {
        var id = Guid.NewGuid();
        Entity a = new ClienteFake(id);
        Entity b = new OutraEntidadeFake(id);

        a.Equals(b).Should().BeFalse();
    }

    [Fact]
    public void Igualdade_IdsDiferentes_NaoSaoIguais()
    {
        var a = new ClienteFake(Guid.NewGuid());
        var b = new ClienteFake(Guid.NewGuid());

        (a != b).Should().BeTrue();
    }

    [Fact]
    public void Igualdade_ComparadoComNull_NaoLancaENaoEIgual()
    {
        var a = new ClienteFake(Guid.NewGuid());

        (a == null).Should().BeFalse();
        a!.Equals(null).Should().BeFalse();
    }
}
