using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>PROJ-54 — atribuição tardia de uma demanda nascida sem_responsavel (modo Manual, responsavel_obrigatorio=false).</summary>
public sealed record AtribuirResponsavelDemandaCommand(Guid DemandaId, Guid ResponsavelId) : IRequest<Result>;

public sealed class AtribuirResponsavelDemandaCommandValidator : AbstractValidator<AtribuirResponsavelDemandaCommand>
{
    public AtribuirResponsavelDemandaCommandValidator() => RuleFor(x => x.ResponsavelId).NotEmpty();
}

public sealed class AtribuirResponsavelDemandaCommandHandler(
    IDemandaRepository demandaRepository, IVerificadorUsuario verificadorUsuario, IUnitOfWork unitOfWork)
    : IRequestHandler<AtribuirResponsavelDemandaCommand, Result>
{
    public async Task<Result> Handle(AtribuirResponsavelDemandaCommand request, CancellationToken cancellationToken)
    {
        var demanda = await demandaRepository.ObterPorIdAsync(request.DemandaId, cancellationToken);
        if (demanda is null)
            return Result.Failure("Demanda não encontrada.");

        if (!await verificadorUsuario.ExisteAsync(request.ResponsavelId, cancellationToken))
            return Result.Failure("Usuário não encontrado.");

        var resultado = demanda.AtribuirResponsavel(request.ResponsavelId);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
