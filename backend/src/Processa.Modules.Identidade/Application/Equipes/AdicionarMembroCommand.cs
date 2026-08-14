using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record AdicionarMembroCommand(Guid EquipeId, Guid UsuarioId, Perfil Papel) : IRequest<Result<Guid>>;

public sealed class AdicionarMembroCommandHandler(
    IEquipeRepository equipeRepository,
    IMembroEquipeRepository membroEquipeRepository,
    IUsuarioRepository usuarioRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarMembroCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdicionarMembroCommand request, CancellationToken cancellationToken)
    {
        var equipe = await equipeRepository.ObterPorIdAsync(request.EquipeId, cancellationToken);
        if (equipe is null)
            return Result.Failure<Guid>("Equipe não encontrada.");

        // Escopado por tenant (Global Query Filter) — um usuário de outro tenant
        // nunca é encontrado aqui, mesmo que o EquipeId seja válido.
        var usuario = await usuarioRepository.ObterPorIdAsync(request.UsuarioId, cancellationToken);
        if (usuario is null)
            return Result.Failure<Guid>("Usuário não encontrado.");

        var membroExistente = await membroEquipeRepository.ObterAsync(request.EquipeId, request.UsuarioId, cancellationToken);
        if (membroExistente is not null)
            return Result.Failure<Guid>("Este usuário já é membro desta equipe.");

        var membroResult = MembroEquipe.Adicionar(tenantContext.TenantId, request.EquipeId, request.UsuarioId, request.Papel);
        if (membroResult.IsFailure)
            return Result.Failure<Guid>(membroResult.Error!);

        await membroEquipeRepository.AddAsync(membroResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(membroResult.Value.Id);
    }
}
