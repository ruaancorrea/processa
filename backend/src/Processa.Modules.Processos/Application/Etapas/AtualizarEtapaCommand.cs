using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Etapas;

public sealed record AtualizarEtapaCommand(Guid EtapaId, string Nome, string? Descricao, int Ordem, ConfiguracaoEtapa? Configuracao)
    : IRequest<Result>;

public sealed class AtualizarEtapaCommandValidator : AbstractValidator<AtualizarEtapaCommand>
{
    public AtualizarEtapaCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);

        // Só a validação interna da Configuracao (URL válida, ramos não-vazios etc.) dá
        // pra espelhar aqui — Tipo não está neste comando (é imutável, mesmo gap já
        // documentado em AtualizarCampoPersonalizadoCommand/Sprint 3), então a regra
        // "este tipo aceita/exige configuração" só existe no Domain (Etapa.AtualizarDados).
        RuleFor(x => x.Configuracao)
            .Must(c => c!.Validar().IsSuccess)
            .WithMessage(x => x.Configuracao!.Validar().Error)
            .When(x => x.Configuracao is not null);
    }
}

public sealed class AtualizarEtapaCommandHandler(IEtapaRepository etapaRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarEtapaCommand, Result>
{
    public async Task<Result> Handle(AtualizarEtapaCommand request, CancellationToken cancellationToken)
    {
        var etapa = await etapaRepository.ObterPorIdAsync(request.EtapaId, cancellationToken);
        if (etapa is null)
            return Result.Failure("Etapa não encontrada.");

        var resultado = etapa.AtualizarDados(request.Nome, request.Descricao, request.Ordem, request.Configuracao);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
