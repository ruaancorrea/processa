using FluentValidation;

namespace Processa.Modules.Identidade.Application.Auth;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Senha).NotEmpty();
    }
}
