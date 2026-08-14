using FluentValidation;
using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Contatos;

public sealed record AdicionarContatoCommand(Guid ClienteId, string Nome, string? Email, string? Telefone, string? Celular)
    : IRequest<Result<Guid>>;

public sealed class AdicionarContatoCommandValidator : AbstractValidator<AdicionarContatoCommand>
{
    public AdicionarContatoCommandValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Email) || !string.IsNullOrWhiteSpace(x.Telefone) || !string.IsNullOrWhiteSpace(x.Celular))
            .WithMessage("Informe ao menos um meio de contato (e-mail, telefone ou celular).")
            .WithName("Contato");
    }
}

public sealed class AdicionarContatoCommandHandler(
    IContatoClienteRepository contatoClienteRepository,
    IClienteRepository clienteRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarContatoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdicionarContatoCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure<Guid>("Cliente não encontrado.");

        var contatoResult = ContatoCliente.Criar(
            tenantContext.TenantId, request.ClienteId, request.Nome, request.Email, request.Telefone, request.Celular);
        if (contatoResult.IsFailure)
            return Result.Failure<Guid>(contatoResult.Error!);

        await contatoClienteRepository.AddAsync(contatoResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(contatoResult.Value.Id);
    }
}
