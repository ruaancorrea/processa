using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record RemoverMembroCommand(Guid EquipeId, Guid UsuarioId) : IRequest<Result>;

public sealed class RemoverMembroCommandHandler(IMembroEquipeRepository membroEquipeRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoverMembroCommand, Result>
{
    public async Task<Result> Handle(RemoverMembroCommand request, CancellationToken cancellationToken)
    {
        var membro = await membroEquipeRepository.ObterAsync(request.EquipeId, request.UsuarioId, cancellationToken);
        if (membro is null)
            return Result.Failure("Este usuário não é membro desta equipe.");

        membroEquipeRepository.Remover(membro);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
