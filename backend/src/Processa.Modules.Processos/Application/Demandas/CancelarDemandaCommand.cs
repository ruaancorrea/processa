using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record CancelarDemandaCommand(Guid DemandaId) : IRequest<Result>;

public sealed class CancelarDemandaCommandHandler(IDemandaRepository demandaRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<CancelarDemandaCommand, Result>
{
    public async Task<Result> Handle(CancelarDemandaCommand request, CancellationToken cancellationToken)
    {
        var demanda = await demandaRepository.ObterPorIdAsync(request.DemandaId, cancellationToken);
        if (demanda is null)
            return Result.Failure("Demanda não encontrada.");

        var resultado = demanda.Cancelar();
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
