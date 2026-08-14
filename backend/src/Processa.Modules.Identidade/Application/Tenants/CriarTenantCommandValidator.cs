using FluentValidation;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Tenants;

public sealed class CriarTenantCommandValidator : AbstractValidator<CriarTenantCommand>
{
    public CriarTenantCommandValidator()
    {
        RuleFor(x => x.NomeEscritorio).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty()
            .Must(Cnpj.EhValido).WithMessage("O CNPJ informado não é válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj));
        RuleFor(x => x.NomeAdmin).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EmailAdmin).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(x => x.Senha).NotEmpty().MinimumLength(8)
            .WithMessage("A senha deve ter pelo menos 8 caracteres.");
    }
}
