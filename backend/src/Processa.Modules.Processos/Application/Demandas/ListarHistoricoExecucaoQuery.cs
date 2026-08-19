using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record HistoricoResumo(Guid Id, TipoEventoHistorico TipoEvento, string Dados, Guid? UsuarioId, DateTimeOffset CreatedAt);

public sealed record ListarHistoricoExecucaoQuery(Guid ExecucaoEtapaId) : IRequest<List<HistoricoResumo>>;

public sealed class ListarHistoricoExecucaoQueryHandler(IHistoricoExecucaoEtapaRepository historicoRepository)
    : IRequestHandler<ListarHistoricoExecucaoQuery, List<HistoricoResumo>>
{
    public async Task<List<HistoricoResumo>> Handle(ListarHistoricoExecucaoQuery request, CancellationToken cancellationToken)
    {
        var historico = await historicoRepository.ListarPorExecucaoAsync(request.ExecucaoEtapaId, cancellationToken);
        return historico.Select(h => new HistoricoResumo(h.Id, h.TipoEvento, h.Dados, h.UsuarioId, h.CreatedAt)).ToList();
    }
}
