using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record ExecucaoEtapaResumo(
    Guid Id, Guid EtapaId, Guid? ResponsavelId, StatusExecucaoEtapa Status, DateTimeOffset IniciadoEm, DateTimeOffset? ConcluidoEm);

public sealed record DemandaDetalhe(
    Guid Id, Guid TipoProcessoId, Guid FluxoAtivoId, Guid ClienteId, Guid? ResponsavelId, StatusDemanda Status,
    Prioridade Prioridade, int PercentualConclusao, DateTimeOffset DataInicio, DateTimeOffset? DataFimPrevista,
    DateTimeOffset? DataFimReal, Guid? EtapaAtualId, Guid? DemandaPaiId, List<ExecucaoEtapaResumo> Execucoes);

public sealed record ObterDemandaPorIdQuery(Guid DemandaId) : IRequest<Result<DemandaDetalhe>>;

public sealed class ObterDemandaPorIdQueryHandler(IDemandaRepository demandaRepository, IExecucaoEtapaRepository execucaoEtapaRepository)
    : IRequestHandler<ObterDemandaPorIdQuery, Result<DemandaDetalhe>>
{
    public async Task<Result<DemandaDetalhe>> Handle(ObterDemandaPorIdQuery request, CancellationToken cancellationToken)
    {
        var demanda = await demandaRepository.ObterPorIdAsync(request.DemandaId, cancellationToken);
        if (demanda is null)
            return Result.Failure<DemandaDetalhe>("Demanda não encontrada.");

        var execucoes = await execucaoEtapaRepository.ListarPorDemandaAsync(demanda.Id, cancellationToken);

        return Result.Success(new DemandaDetalhe(
            demanda.Id, demanda.TipoProcessoId, demanda.FluxoAtivoId, demanda.ClienteId, demanda.ResponsavelId, demanda.Status,
            demanda.Prioridade, demanda.PercentualConclusao, demanda.DataInicio, demanda.DataFimPrevista, demanda.DataFimReal,
            demanda.EtapaAtualId, demanda.DemandaPaiId,
            execucoes.OrderBy(e => e.CreatedAt)
                .Select(e => new ExecucaoEtapaResumo(e.Id, e.EtapaId, e.ResponsavelId, e.Status, e.IniciadoEm, e.ConcluidoEm))
                .ToList()));
    }
}
