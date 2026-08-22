using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IDemandaRepository
{
    Task AddAsync(Demanda demanda, CancellationToken ct = default);
    Task<Demanda?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Demanda>> ListarAsync(CancellationToken ct = default);
    Task<int> ContarAtivasPorResponsavelAsync(Guid responsavelId, CancellationToken ct = default);

    /// <summary>Lista paginada/filtrada/ordenada (PROJ-58). Ordenacao vazia/nula aplica o padrão: sem responsável primeiro, depois DataInicio ascendente.</summary>
    Task<ResultadoPaginado<Demanda>> ListarComFiltroAsync(
        FiltroDemandas filtro, IReadOnlyList<OrdenacaoDemanda> ordenacao, int pagina, int tamanhoPagina, CancellationToken ct = default);

    /// <summary>Demandas de um TipoProcesso ainda em curso, com EtapaAtualId definido — a matéria-prima do board kanban (PROJ-57).</summary>
    Task<List<Demanda>> ListarParaKanbanAsync(Guid tipoProcessoId, CancellationToken ct = default);

    Task<List<Demanda>> ListarPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}
