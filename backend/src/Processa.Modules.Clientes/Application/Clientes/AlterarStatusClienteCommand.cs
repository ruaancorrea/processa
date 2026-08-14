using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed record SuspenderClienteCommand(Guid ClienteId) : IRequest<Result>;
public sealed record InativarClienteCommand(Guid ClienteId) : IRequest<Result>;
public sealed record ReativarClienteCommand(Guid ClienteId) : IRequest<Result>;

public sealed class SuspenderClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<SuspenderClienteCommand, Result>
{
    public async Task<Result> Handle(SuspenderClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure("Cliente não encontrado.");

        cliente.Suspender();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class InativarClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<InativarClienteCommand, Result>
{
    public async Task<Result> Handle(InativarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure("Cliente não encontrado.");

        cliente.Inativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ReativarClienteCommandHandler(IClienteRepository clienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ReativarClienteCommand, Result>
{
    public async Task<Result> Handle(ReativarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure("Cliente não encontrado.");

        cliente.Reativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
