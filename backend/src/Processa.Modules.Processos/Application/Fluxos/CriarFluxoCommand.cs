using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Fluxos;

/// <summary>
/// FluxoPadraoSolicitado é uma preferência, não garantia — o primeiro fluxo de um
/// tipo de processo vira padrão automaticamente independente do valor pedido (não
/// pode existir tipo de processo sem fluxo padrão nenhum). Pedir padrão explicitamente
/// pra um fluxo que não é o primeiro desmarca o padrão anterior na mesma transação.
/// </summary>
public sealed record CriarFluxoCommand(Guid TipoProcessoId, string Nome, string? Descricao, bool FluxoPadraoSolicitado)
    : IRequest<Result<Guid>>;

public sealed class CriarFluxoCommandValidator : AbstractValidator<CriarFluxoCommand>
{
    public CriarFluxoCommandValidator()
    {
        RuleFor(x => x.TipoProcessoId).NotEmpty();
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(1000);
    }
}

public sealed class CriarFluxoCommandHandler(
    IFluxoRepository fluxoRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarFluxoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarFluxoCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure<Guid>("Tipo de processo não encontrado.");

        var fluxoAtualPadrao = await fluxoRepository.ObterPadraoAsync(request.TipoProcessoId, cancellationToken);
        var seraOPadrao = fluxoAtualPadrao is null || request.FluxoPadraoSolicitado;

        var fluxoResult = Fluxo.Criar(tenantContext.TenantId, request.TipoProcessoId, request.Nome, request.Descricao, seraOPadrao);
        if (fluxoResult.IsFailure)
            return Result.Failure<Guid>(fluxoResult.Error!);

        // Desmarcar e commitar ANTES de inserir o novo padrão — o índice único parcial
        // (ver FluxoConfiguration) não tolera duas linhas com padrão=true ao mesmo tempo,
        // e um único SaveChanges não garante UPDATE (desmarcar) antes de INSERT (o novo
        // fluxo já nascendo padrão=true). Ver comentário equivalente em DefinirFluxoPadraoCommand.
        if (seraOPadrao && fluxoAtualPadrao is not null)
        {
            fluxoAtualPadrao.DesmarcarComoPadrao();
            await unitOfWork.SalvarAsync(cancellationToken);
        }

        await fluxoRepository.AddAsync(fluxoResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(fluxoResult.Value.Id);
    }
}
