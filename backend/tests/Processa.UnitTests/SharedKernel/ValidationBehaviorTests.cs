using FluentAssertions;
using FluentValidation;
using MediatR;
using Processa.Shared.Kernel;
using Xunit;
using ValidationException = Processa.Shared.Kernel.ValidationException;

namespace Processa.UnitTests.SharedKernel;

public class ValidationBehaviorTests
{
    private sealed record ComandoFake(string Nome) : IRequest<string>;

    private sealed class ComandoFakeValidator : AbstractValidator<ComandoFake>
    {
        public ComandoFakeValidator() => RuleFor(c => c.Nome).NotEmpty().WithMessage("Nome é obrigatório.");
    }

    private static ValidationBehavior<ComandoFake, string> CriarBehavior(params IValidator<ComandoFake>[] validators) =>
        new(validators);

    [Fact]
    public async Task Handle_SemValidadoresRegistrados_ChamaNextDireto()
    {
        var behavior = CriarBehavior();

        var resultado = await behavior.Handle(new ComandoFake("qualquer"), () => Task.FromResult("ok"), CancellationToken.None);

        resultado.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_RequestValido_ChamaNext()
    {
        var behavior = CriarBehavior(new ComandoFakeValidator());

        var resultado = await behavior.Handle(new ComandoFake("Ana"), () => Task.FromResult("ok"), CancellationToken.None);

        resultado.Should().Be("ok");
    }

    [Fact]
    public async Task Handle_RequestInvalido_LancaValidationExceptionComOsErros()
    {
        var behavior = CriarBehavior(new ComandoFakeValidator());

        var act = () => behavior.Handle(new ComandoFake(""), () => Task.FromResult("nunca chega aqui"), CancellationToken.None);

        var excecao = await act.Should().ThrowAsync<ValidationException>();
        excecao.Which.Errors.Should().ContainKey("Nome");
        excecao.Which.Errors["Nome"].Should().Contain("Nome é obrigatório.");
    }
}
