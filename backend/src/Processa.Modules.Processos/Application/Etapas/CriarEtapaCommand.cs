using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Etapas;

public sealed record CriarEtapaCommand(Guid FluxoId, string Nome, string? Descricao, TipoEtapa Tipo, int Ordem, ConfiguracaoEtapa? Configuracao)
    : IRequest<Result<Guid>>;

public sealed class CriarEtapaCommandValidator : AbstractValidator<CriarEtapaCommand>
{
    public CriarEtapaCommandValidator()
    {
        RuleFor(x => x.FluxoId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);

        // Espelha Etapa.ValidarConfiguracaoParaTipo (Domain) — mesma disciplina de
        // RFC 9457 do resto do projeto: sem isso, essa falha vira um 422 ad-hoc sem
        // o dict errors por campo.
        RuleFor(x => x.Configuracao).Null()
            .When(x => x.Tipo is TipoEtapa.Comum or TipoEtapa.Conclusao)
            .WithMessage("Etapa Comum ou de Conclusão não aceita configuração.");
        RuleFor(x => x.Configuracao).NotNull()
            .When(x => x.Tipo is not (TipoEtapa.Comum or TipoEtapa.Conclusao))
            .WithMessage("Este tipo de etapa exige configuração.");

        // Reaproveita ConfiguracaoEtapa.Validar() (o mesmo método que Etapa.Criar chama
        // no Domain) em vez de duplicar regra por regra (URL válida, ramos não-vazios,
        // etc.) pros 6 tipos — mesma disciplina RFC 9457, sem duplicar lógica de negócio.
        RuleFor(x => x.Configuracao)
            .Must(c => c!.Validar().IsSuccess)
            .WithMessage(x => x.Configuracao!.Validar().Error)
            .When(x => x.Configuracao is not null);
    }
}

public sealed class CriarEtapaCommandHandler(
    IEtapaRepository etapaRepository, IFluxoRepository fluxoRepository, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    : IRequestHandler<CriarEtapaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarEtapaCommand request, CancellationToken cancellationToken)
    {
        var fluxo = await fluxoRepository.ObterPorIdAsync(request.FluxoId, cancellationToken);
        if (fluxo is null)
            return Result.Failure<Guid>("Fluxo não encontrado.");

        var etapaResult = Etapa.Criar(
            tenantContext.TenantId, request.FluxoId, request.Nome, request.Descricao, request.Tipo, request.Ordem, request.Configuracao);
        if (etapaResult.IsFailure)
            return Result.Failure<Guid>(etapaResult.Error!);

        await etapaRepository.AddAsync(etapaResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(etapaResult.Value.Id);
    }
}
