using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Campos;

public sealed record RemoverCampoPersonalizadoCommand(Guid CampoId) : IRequest<Result>;

public sealed class RemoverCampoPersonalizadoCommandHandler(ICampoPersonalizadoRepository campoPersonalizadoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoverCampoPersonalizadoCommand, Result>
{
    public async Task<Result> Handle(RemoverCampoPersonalizadoCommand request, CancellationToken cancellationToken)
    {
        var campo = await campoPersonalizadoRepository.ObterPorIdAsync(request.CampoId, cancellationToken);
        if (campo is null)
            return Result.Failure("Campo personalizado não encontrado.");

        campoPersonalizadoRepository.Remover(campo);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
