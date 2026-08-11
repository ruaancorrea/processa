using FluentAssertions;
using Processa.Shared.Kernel;
using Xunit;

namespace Processa.UnitTests.SharedKernel;

public class ResultTests
{
    [Fact]
    public void Success_SemValor_EhSucessoSemErro()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_SemValor_EhFalhaComErro()
    {
        var result = Result.Failure("algo deu errado");

        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("algo deu errado");
    }

    [Fact]
    public void Success_ComValor_ExpoeValor()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ComValor_AcessarValorLanca()
    {
        var result = Result.Failure<int>("inválido");

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ConversaoImplicita_DeValorParaResultSucesso()
    {
        Result<string> result = "ok";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ok");
    }
}
