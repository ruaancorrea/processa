using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record DesativarEquipeCommand(Guid EquipeId) : IRequest<Result>;

public sealed class DesativarEquipeCommandHandler(IEquipeRepository equipeRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DesativarEquipeCommand, Result>
{
    public async Task<Result> Handle(DesativarEquipeCommand request, CancellationToken cancellationToken)
    {
        var equipe = await equipeRepository.ObterPorIdAsync(request.EquipeId, cancellationToken);
        if (equipe is null)
            return Result.Failure("Equipe não encontrada.");

        equipe.Desativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed record ReativarEquipeCommand(Guid EquipeId) : IRequest<Result>;

public sealed class ReativarEquipeCommandHandler(IEquipeRepository equipeRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<ReativarEquipeCommand, Result>
{
    public async Task<Result> Handle(ReativarEquipeCommand request, CancellationToken cancellationToken)
    {
        var equipe = await equipeRepository.ObterPorIdAsync(request.EquipeId, cancellationToken);
        if (equipe is null)
            return Result.Failure("Equipe não encontrada.");

        equipe.Reativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
