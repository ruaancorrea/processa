using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record AtualizarPrioridadeDemandaCommand(Guid DemandaId, Prioridade Prioridade) : IRequest<Result>;

public sealed class AtualizarPrioridadeDemandaCommandValidator : AbstractValidator<AtualizarPrioridadeDemandaCommand>
{
    public AtualizarPrioridadeDemandaCommandValidator() => RuleFor(x => x.Prioridade).IsInEnum();
}

public sealed class AtualizarPrioridadeDemandaCommandHandler(IDemandaRepository demandaRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarPrioridadeDemandaCommand, Result>
{
    public async Task<Result> Handle(AtualizarPrioridadeDemandaCommand request, CancellationToken cancellationToken)
    {
        var demanda = await demandaRepository.ObterPorIdAsync(request.DemandaId, cancellationToken);
        if (demanda is null)
            return Result.Failure("Demanda não encontrada.");

        demanda.AlterarPrioridade(request.Prioridade);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
