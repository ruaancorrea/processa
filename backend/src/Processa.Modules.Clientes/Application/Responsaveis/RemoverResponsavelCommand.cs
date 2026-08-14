using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Responsaveis;

public sealed record RemoverResponsavelCommand(Guid ResponsavelId) : IRequest<Result>;

public sealed class RemoverResponsavelCommandHandler(IResponsavelClienteRepository responsavelClienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoverResponsavelCommand, Result>
{
    public async Task<Result> Handle(RemoverResponsavelCommand request, CancellationToken cancellationToken)
    {
        var responsavel = await responsavelClienteRepository.ObterPorIdAsync(request.ResponsavelId, cancellationToken);
        if (responsavel is null || !responsavel.EstaAtivo)
            return Result.Failure("Vínculo de responsável não encontrado.");

        responsavel.Remover();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
