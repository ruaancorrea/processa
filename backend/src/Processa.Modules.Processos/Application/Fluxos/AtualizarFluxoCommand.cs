using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Fluxos;

public sealed record AtualizarFluxoCommand(Guid FluxoId, string Nome, string? Descricao) : IRequest<Result>;

public sealed class AtualizarFluxoCommandValidator : AbstractValidator<AtualizarFluxoCommand>
{
    public AtualizarFluxoCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
    }
}

public sealed class AtualizarFluxoCommandHandler(IFluxoRepository fluxoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarFluxoCommand, Result>
{
    public async Task<Result> Handle(AtualizarFluxoCommand request, CancellationToken cancellationToken)
    {
        var fluxo = await fluxoRepository.ObterPorIdAsync(request.FluxoId, cancellationToken);
        if (fluxo is null)
            return Result.Failure("Fluxo não encontrado.");

        var resultado = fluxo.AtualizarDados(request.Nome, request.Descricao);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
