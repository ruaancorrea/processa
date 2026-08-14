using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record AtualizarEquipeCommand(Guid EquipeId, string NomeEquipe, string? Descricao) : IRequest<Result>;

public sealed class AtualizarEquipeCommandHandler(IEquipeRepository equipeRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarEquipeCommand, Result>
{
    public async Task<Result> Handle(AtualizarEquipeCommand request, CancellationToken cancellationToken)
    {
        var equipe = await equipeRepository.ObterPorIdAsync(request.EquipeId, cancellationToken);
        if (equipe is null)
            return Result.Failure("Equipe não encontrada.");

        var resultado = equipe.AtualizarDados(request.NomeEquipe, request.Descricao);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
