using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record DemandaResumo(
    Guid Id, Guid TipoProcessoId, Guid ClienteId, Guid? ResponsavelId, StatusDemanda Status, Prioridade Prioridade,
    int PercentualConclusao, DateTimeOffset DataInicio, DateTimeOffset? DataFimPrevista);

public sealed record ListarDemandasQuery : IRequest<List<DemandaResumo>>;

public sealed class ListarDemandasQueryHandler(IDemandaRepository demandaRepository) : IRequestHandler<ListarDemandasQuery, List<DemandaResumo>>
{
    public async Task<List<DemandaResumo>> Handle(ListarDemandasQuery request, CancellationToken cancellationToken)
    {
        var demandas = await demandaRepository.ListarAsync(cancellationToken);
        return demandas.Select(d => new DemandaResumo(
            d.Id, d.TipoProcessoId, d.ClienteId, d.ResponsavelId, d.Status, d.Prioridade,
            d.PercentualConclusao, d.DataInicio, d.DataFimPrevista)).ToList();
    }
}
