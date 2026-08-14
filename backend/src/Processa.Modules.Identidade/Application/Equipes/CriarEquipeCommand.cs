using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

/// <summary>
/// Sem TenantId no comando — nunca aceito do cliente (um request forjado poderia
/// criar recursos em outro tenant). Resolvido server-side via ITenantContext no
/// handler, mesma fonte que o Global Query Filter usa para leitura.
/// </summary>
public sealed record CriarEquipeCommand(string NomeEquipe, string? Descricao) : IRequest<Result<Guid>>;

public sealed class CriarEquipeCommandHandler(
    IEquipeRepository equipeRepository, IUnitOfWork unitOfWork, ITenantContext tenantContext)
    : IRequestHandler<CriarEquipeCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarEquipeCommand request, CancellationToken cancellationToken)
    {
        var equipeResult = Equipe.Criar(tenantContext.TenantId, request.NomeEquipe, request.Descricao);
        if (equipeResult.IsFailure)
            return Result.Failure<Guid>(equipeResult.Error!);

        await equipeRepository.AddAsync(equipeResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(equipeResult.Value.Id);
    }
}
