using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Campos;

public sealed record CriarCampoPersonalizadoCommand(
    Guid TipoProcessoId, string Nome, TipoCampoPersonalizado Tipo, List<string>? Opcoes, bool Obrigatorio, int Ordem)
    : IRequest<Result<Guid>>;

public sealed class CriarCampoPersonalizadoCommandValidator : AbstractValidator<CriarCampoPersonalizadoCommand>
{
    public CriarCampoPersonalizadoCommandValidator()
    {
        RuleFor(x => x.TipoProcessoId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Ordem).GreaterThanOrEqualTo(0);

        // Espelha CampoPersonalizado.Criar (Domain) — sem isso, essa falha vira um 422
        // "solto" (Results.Problem ad-hoc) em vez do formato RFC 9457 com errors.Opcoes
        // exigido por docs/04-api/convencoes-api.md. Mesma classe de achado do CNPJ
        // (Sprint 1) e papel=Admin/contato sem meio de contato (Sprint 2).
        RuleFor(x => x)
            .Must(x => x.Tipo != TipoCampoPersonalizado.Lista || x.Opcoes is { Count: > 0 })
            .WithMessage("Campo do tipo lista exige ao menos uma opção.")
            .WithName("Opcoes");

        RuleFor(x => x)
            .Must(x => x.Tipo == TipoCampoPersonalizado.Lista || x.Opcoes is not { Count: > 0 })
            .WithMessage("Opções só fazem sentido para campo do tipo lista.")
            .WithName("Opcoes");
    }
}

public sealed class CriarCampoPersonalizadoCommandHandler(
    ICampoPersonalizadoRepository campoPersonalizadoRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarCampoPersonalizadoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarCampoPersonalizadoCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure<Guid>("Tipo de processo não encontrado.");

        var campoResult = CampoPersonalizado.Criar(
            tenantContext.TenantId, request.TipoProcessoId, request.Nome, request.Tipo, request.Opcoes, request.Obrigatorio, request.Ordem);
        if (campoResult.IsFailure)
            return Result.Failure<Guid>(campoResult.Error!);

        await campoPersonalizadoRepository.AddAsync(campoResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(campoResult.Value.Id);
    }
}
