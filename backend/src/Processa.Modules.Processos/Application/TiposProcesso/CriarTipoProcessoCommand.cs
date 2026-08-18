using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record CriarTipoProcessoCommand(
    Guid EquipeId,
    string Nome,
    string? Descricao,
    bool ResponsavelObrigatorio,
    ModoAtribuicao ModoAtribuicao,
    Guid? ResponsavelFixoId) : IRequest<Result<Guid>>;

public sealed class CriarTipoProcessoCommandHandler(
    ITipoProcessoRepository tipoProcessoRepository,
    IVerificadorEquipe verificadorEquipe,
    IVerificadorMembroEquipe verificadorMembroEquipe,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarTipoProcessoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarTipoProcessoCommand request, CancellationToken cancellationToken)
    {
        if (!await verificadorEquipe.ExisteAsync(request.EquipeId, cancellationToken))
            return Result.Failure<Guid>("Equipe não encontrada.");

        if (request.ModoAtribuicao == ModoAtribuicao.Fixo)
        {
            if (request.ResponsavelFixoId is not { } responsavelFixoId
                || !await verificadorMembroEquipe.EhMembroAsync(request.EquipeId, responsavelFixoId, cancellationToken))
                return Result.Failure<Guid>("O responsável fixo precisa ser membro da equipe informada.");
        }

        var tipoProcessoResult = TipoProcesso.Criar(
            tenantContext.TenantId, request.EquipeId, request.Nome, request.Descricao,
            request.ResponsavelObrigatorio, request.ModoAtribuicao, request.ResponsavelFixoId);
        if (tipoProcessoResult.IsFailure)
            return Result.Failure<Guid>(tipoProcessoResult.Error!);

        await tipoProcessoRepository.AddAsync(tipoProcessoResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(tipoProcessoResult.Value.Id);
    }
}
