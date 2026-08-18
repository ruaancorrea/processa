using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Etapas;

public sealed record RemoverEtapaCommand(Guid EtapaId) : IRequest<Result>;

public sealed class RemoverEtapaCommandHandler(IEtapaRepository etapaRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoverEtapaCommand, Result>
{
    public async Task<Result> Handle(RemoverEtapaCommand request, CancellationToken cancellationToken)
    {
        var etapa = await etapaRepository.ObterPorIdAsync(request.EtapaId, cancellationToken);
        if (etapa is null)
            return Result.Failure("Etapa não encontrada.");

        etapaRepository.Remover(etapa);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
