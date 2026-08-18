using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Fluxos;

public sealed record DefinirFluxoPadraoCommand(Guid FluxoId) : IRequest<Result>;

public sealed class DefinirFluxoPadraoCommandHandler(IFluxoRepository fluxoRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DefinirFluxoPadraoCommand, Result>
{
    public async Task<Result> Handle(DefinirFluxoPadraoCommand request, CancellationToken cancellationToken)
    {
        var fluxo = await fluxoRepository.ObterPorIdAsync(request.FluxoId, cancellationToken);
        if (fluxo is null)
            return Result.Failure("Fluxo não encontrado.");

        if (fluxo.FluxoPadrao)
            return Result.Success();

        // Duas gravações, não uma: o índice único parcial (tipo_processo_id) WHERE
        // fluxo_padrao = true (ver FluxoConfiguration) não tolera as DUAS linhas com
        // padrão=true ao mesmo tempo, e um único SaveChanges não garante que o UPDATE
        // de desmarcar rode antes do de marcar (achado real: ordem de statement do EF
        // Core pra entidades sem relação de FK entre si não é determinística aqui —
        // um teste de integração pegou a violação de unicidade intermitente).
        var fluxoAtualPadrao = await fluxoRepository.ObterPadraoAsync(fluxo.TipoProcessoId, cancellationToken);
        if (fluxoAtualPadrao is not null)
        {
            fluxoAtualPadrao.DesmarcarComoPadrao();
            await unitOfWork.SalvarAsync(cancellationToken);
        }

        fluxo.MarcarComoPadrao();
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
