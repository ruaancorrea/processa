using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Campos;

public sealed record AtualizarCampoPersonalizadoCommand(Guid CampoId, string Nome, List<string>? Opcoes, bool Obrigatorio, int Ordem)
    : IRequest<Result>;

public sealed class AtualizarCampoPersonalizadoCommandValidator : AbstractValidator<AtualizarCampoPersonalizadoCommand>
{
    public AtualizarCampoPersonalizadoCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);
    }
}

public sealed class AtualizarCampoPersonalizadoCommandHandler(ICampoPersonalizadoRepository campoPersonalizadoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarCampoPersonalizadoCommand, Result>
{
    public async Task<Result> Handle(AtualizarCampoPersonalizadoCommand request, CancellationToken cancellationToken)
    {
        var campo = await campoPersonalizadoRepository.ObterPorIdAsync(request.CampoId, cancellationToken);
        if (campo is null)
            return Result.Failure("Campo personalizado não encontrado.");

        var resultado = campo.AtualizarDados(request.Nome, request.Opcoes, request.Obrigatorio, request.Ordem);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
