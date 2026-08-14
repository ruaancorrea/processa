using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Contatos;

public sealed record DesativarContatoCommand(Guid ContatoId) : IRequest<Result>;
public sealed record ReativarContatoCommand(Guid ContatoId) : IRequest<Result>;

public sealed class DesativarContatoCommandHandler(IContatoClienteRepository contatoClienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DesativarContatoCommand, Result>
{
    public async Task<Result> Handle(DesativarContatoCommand request, CancellationToken cancellationToken)
    {
        var contato = await contatoClienteRepository.ObterPorIdAsync(request.ContatoId, cancellationToken);
        if (contato is null)
            return Result.Failure("Contato não encontrado.");

        contato.Desativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class ReativarContatoCommandHandler(IContatoClienteRepository contatoClienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ReativarContatoCommand, Result>
{
    public async Task<Result> Handle(ReativarContatoCommand request, CancellationToken cancellationToken)
    {
        var contato = await contatoClienteRepository.ObterPorIdAsync(request.ContatoId, cancellationToken);
        if (contato is null)
            return Result.Failure("Contato não encontrado.");

        contato.Reativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
