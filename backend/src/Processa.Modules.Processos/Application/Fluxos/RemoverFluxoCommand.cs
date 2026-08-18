using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Fluxos;

public sealed record RemoverFluxoCommand(Guid FluxoId) : IRequest<Result>;

public sealed class RemoverFluxoCommandHandler(IFluxoRepository fluxoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<RemoverFluxoCommand, Result>
{
    public async Task<Result> Handle(RemoverFluxoCommand request, CancellationToken cancellationToken)
    {
        var fluxo = await fluxoRepository.ObterPorIdAsync(request.FluxoId, cancellationToken);
        if (fluxo is null)
            return Result.Failure("Fluxo não encontrado.");

        if (fluxo.FluxoPadrao)
        {
            var outrosFluxos = await fluxoRepository.ListarPorTipoProcessoAsync(fluxo.TipoProcessoId, cancellationToken);
            if (outrosFluxos.Any(f => f.Id != fluxo.Id))
                return Result.Failure("Defina outro fluxo como padrão antes de remover este.");
        }

        fluxoRepository.Remover(fluxo);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
