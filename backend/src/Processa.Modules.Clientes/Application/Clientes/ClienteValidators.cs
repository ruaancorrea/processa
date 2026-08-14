using FluentValidation;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteCommandValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Cnpj).NotEmpty()
            .Must(Cnpj.EhValido).WithMessage("O CNPJ informado não é válido.")
            .When(x => !string.IsNullOrWhiteSpace(x.Cnpj));
        RuleFor(x => x.CodigoExterno).MaximumLength(100);
        RuleFor(x => x.RegimeTributario).IsInEnum();
        RuleFor(x => x.DataEntrada).NotEqual(default(DateOnly)).WithMessage("A data de entrada é obrigatória.");
    }
}

public sealed class AtualizarClienteCommandValidator : AbstractValidator<AtualizarClienteCommand>
{
    public AtualizarClienteCommandValidator()
    {
        RuleFor(x => x.RazaoSocial).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CodigoExterno).MaximumLength(100);
        RuleFor(x => x.RegimeTributario).IsInEnum();
    }
}
