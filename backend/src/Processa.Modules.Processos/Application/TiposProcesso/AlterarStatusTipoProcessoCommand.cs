using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record AtivarTipoProcessoCommand(Guid TipoProcessoId) : IRequest<Result>;
public sealed record DesativarTipoProcessoCommand(Guid TipoProcessoId) : IRequest<Result>;

public sealed class AtivarTipoProcessoCommandHandler(ITipoProcessoRepository tipoProcessoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtivarTipoProcessoCommand, Result>
{
    public async Task<Result> Handle(AtivarTipoProcessoCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure("Tipo de processo não encontrado.");

        tipoProcesso.Ativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DesativarTipoProcessoCommandHandler(ITipoProcessoRepository tipoProcessoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DesativarTipoProcessoCommand, Result>
{
    public async Task<Result> Handle(DesativarTipoProcessoCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure("Tipo de processo não encontrado.");

        tipoProcesso.Desativar();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
