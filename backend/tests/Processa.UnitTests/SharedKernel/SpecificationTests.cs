using FluentAssertions;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.SharedKernel;

public class SpecificationTests
{
    private sealed record Demanda(string Status, string Prioridade);

    private sealed class StatusEhSpec(string status) : Specification<Demanda>
    {
        public override System.Linq.Expressions.Expression<Func<Demanda, bool>> ToExpression()
            => d => d.Status == status;
    }

    private sealed class PrioridadeEhSpec(string prioridade) : Specification<Demanda>
    {
        public override System.Linq.Expressions.Expression<Func<Demanda, bool>> ToExpression()
            => d => d.Prioridade == prioridade;
    }

    [Fact]
    public void IsSatisfiedBy_CondicaoSimples_Verdadeira()
    {
        var spec = new StatusEhSpec("em_andamento");

        spec.IsSatisfiedBy(new Demanda("em_andamento", "alta")).Should().BeTrue();
        spec.IsSatisfiedBy(new Demanda("concluido", "alta")).Should().BeFalse();
    }

    [Fact]
    public void And_CombinaDuasSpecs()
    {
        var spec = new StatusEhSpec("em_andamento").And(new PrioridadeEhSpec("urgente"));

        spec.IsSatisfiedBy(new Demanda("em_andamento", "urgente")).Should().BeTrue();
        spec.IsSatisfiedBy(new Demanda("em_andamento", "baixa")).Should().BeFalse();
    }

    [Fact]
    public void Not_NegaASpec()
    {
        var spec = new StatusEhSpec("concluido").Not();

        spec.IsSatisfiedBy(new Demanda("em_andamento", "alta")).Should().BeTrue();
        spec.IsSatisfiedBy(new Demanda("concluido", "alta")).Should().BeFalse();
    }
}
