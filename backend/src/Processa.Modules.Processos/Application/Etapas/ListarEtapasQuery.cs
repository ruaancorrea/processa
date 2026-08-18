using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Etapas;

public sealed record EtapaResumo(Guid Id, string Nome, TipoEtapa Tipo, int Ordem);

public sealed record ListarEtapasQuery(Guid FluxoId) : IRequest<List<EtapaResumo>>;

public sealed class ListarEtapasQueryHandler(IEtapaRepository etapaRepository) : IRequestHandler<ListarEtapasQuery, List<EtapaResumo>>
{
    public async Task<List<EtapaResumo>> Handle(ListarEtapasQuery request, CancellationToken cancellationToken)
    {
        var etapas = await etapaRepository.ListarPorFluxoAsync(request.FluxoId, cancellationToken);
        return etapas.OrderBy(e => e.Ordem).Select(e => new EtapaResumo(e.Id, e.Nome, e.Tipo, e.Ordem)).ToList();
    }
}
