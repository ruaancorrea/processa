using Microsoft.EntityFrameworkCore;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure.Repositorios;

public sealed class DemandaRepository(ProcessosDbContext db) : IDemandaRepository
{
    public async Task AddAsync(Demanda demanda, CancellationToken ct = default) =>
        await db.Demandas.AddAsync(demanda, ct);

    public Task<Demanda?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Demandas.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<List<Demanda>> ListarAsync(CancellationToken ct = default) =>
        db.Demandas.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

    public Task<int> ContarAtivasPorResponsavelAsync(Guid responsavelId, CancellationToken ct = default) =>
        db.Demandas.CountAsync(
            d => d.ResponsavelId == responsavelId && d.Status != StatusDemanda.Concluido && d.Status != StatusDemanda.Cancelado, ct);

    public async Task<ResultadoPaginado<Demanda>> ListarComFiltroAsync(
        FiltroDemandas filtro, IReadOnlyList<OrdenacaoDemanda> ordenacao, int pagina, int tamanhoPagina, CancellationToken ct = default)
    {
        var query = AplicarFiltro(db.Demandas.AsQueryable(), filtro);
        var total = await query.CountAsync(ct);

        query = AplicarOrdenacao(query, ordenacao);

        var paginaEfetiva = Math.Max(1, pagina);
        var tamanhoEfetivo = Math.Clamp(tamanhoPagina, 1, 200);
        var itens = await query.Skip((paginaEfetiva - 1) * tamanhoEfetivo).Take(tamanhoEfetivo).ToListAsync(ct);

        return new ResultadoPaginado<Demanda>(itens, total, paginaEfetiva, tamanhoEfetivo);
    }

    public Task<List<Demanda>> ListarParaKanbanAsync(Guid tipoProcessoId, CancellationToken ct = default) =>
        db.Demandas
            .Where(d => d.TipoProcessoId == tipoProcessoId && d.EtapaAtualId != null
                && d.Status != StatusDemanda.Concluido && d.Status != StatusDemanda.Cancelado)
            .ToListAsync(ct);

    public Task<List<Demanda>> ListarPorIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();
        return lista.Count == 0 ? Task.FromResult(new List<Demanda>()) : db.Demandas.Where(d => lista.Contains(d.Id)).ToListAsync(ct);
    }

    private static IQueryable<Demanda> AplicarFiltro(IQueryable<Demanda> query, FiltroDemandas filtro)
    {
        if (filtro.Status is { } status)
            query = query.Where(d => d.Status == status);
        if (filtro.SemResponsavel is true)
            query = query.Where(d => d.ResponsavelId == null);
        else if (filtro.ResponsavelId is { } responsavelId)
            query = query.Where(d => d.ResponsavelId == responsavelId);
        if (filtro.Prioridade is { } prioridade)
            query = query.Where(d => d.Prioridade == prioridade);
        if (filtro.ClienteId is { } clienteId)
            query = query.Where(d => d.ClienteId == clienteId);
        if (filtro.TipoProcessoId is { } tipoProcessoId)
            query = query.Where(d => d.TipoProcessoId == tipoProcessoId);
        if (filtro.EtapaAtualId is { } etapaAtualId)
            query = query.Where(d => d.EtapaAtualId == etapaAtualId);
        if (filtro.DataInicioDe is { } dataInicioDe)
            query = query.Where(d => d.DataInicio >= dataInicioDe);
        if (filtro.DataInicioAte is { } dataInicioAte)
            query = query.Where(d => d.DataInicio <= dataInicioAte);
        if (filtro.DataFimPrevistaDe is { } dataFimPrevistaDe)
            query = query.Where(d => d.DataFimPrevista >= dataFimPrevistaDe);
        if (filtro.DataFimPrevistaAte is { } dataFimPrevistaAte)
            query = query.Where(d => d.DataFimPrevista <= dataFimPrevistaAte);

        return query;
    }

    // Prioridade/Status são gravados como string (ver DemandaConfiguration) — um OrderBy
    // direto ordenaria alfabeticamente pela coluna ("Alta" < "Baixa" < "Media" < "Urgente"),
    // não pela severidade/progressão real. O switch de rank precisa ficar INLINE dentro de
    // cada lambda (não numa chamada a método separado): EF Core traduz um switch/ternário
    // embutido pra CASE WHEN no SQL, mas não consegue "olhar dentro" de uma chamada a um
    // método C# comum — isso só lança InvalidOperationException "could not be translated"
    // em tempo de EXECUÇÃO contra Postgres de verdade; nenhum teste com fake repository
    // pega esse tipo de erro (achado rodando a suíte de integração real, não em unit test).
    private static IQueryable<Demanda> AplicarOrdenacao(IQueryable<Demanda> query, IReadOnlyList<OrdenacaoDemanda> ordenacao)
    {
        if (ordenacao.Count == 0)
            return query.OrderBy(d => d.Status == StatusDemanda.SemResponsavel ? 0 : 1).ThenBy(d => d.DataInicio);

        var ordenada = AplicarPrimeiraChave(query, ordenacao[0]);
        foreach (var chave in ordenacao.Skip(1))
            ordenada = AplicarProximaChave(ordenada, chave);

        return ordenada;
    }

    private static IOrderedQueryable<Demanda> AplicarPrimeiraChave(IQueryable<Demanda> query, OrdenacaoDemanda chave) =>
        (chave.Campo, chave.Descendente) switch
        {
            (CampoOrdenacaoDemanda.DataInicio, false) => query.OrderBy(d => d.DataInicio),
            (CampoOrdenacaoDemanda.DataInicio, true) => query.OrderByDescending(d => d.DataInicio),
            (CampoOrdenacaoDemanda.TempoDecorrido, false) => query.OrderByDescending(d => d.DataInicio),
            (CampoOrdenacaoDemanda.TempoDecorrido, true) => query.OrderBy(d => d.DataInicio),
            (CampoOrdenacaoDemanda.DataFimPrevista, false) => query.OrderBy(d => d.DataFimPrevista),
            (CampoOrdenacaoDemanda.DataFimPrevista, true) => query.OrderByDescending(d => d.DataFimPrevista),
            (CampoOrdenacaoDemanda.Prioridade, false) => query.OrderBy(d => d.Prioridade == Prioridade.Baixa ? 0
                : d.Prioridade == Prioridade.Media ? 1 : d.Prioridade == Prioridade.Alta ? 2 : 3),
            (CampoOrdenacaoDemanda.Prioridade, true) => query.OrderByDescending(d => d.Prioridade == Prioridade.Baixa ? 0
                : d.Prioridade == Prioridade.Media ? 1 : d.Prioridade == Prioridade.Alta ? 2 : 3),
            (CampoOrdenacaoDemanda.Status, false) => query.OrderBy(d => d.Status == StatusDemanda.SemResponsavel ? 0
                : d.Status == StatusDemanda.Pendente ? 1 : d.Status == StatusDemanda.EmAndamento ? 2 : d.Status == StatusDemanda.Concluido ? 3 : 4),
            (CampoOrdenacaoDemanda.Status, true) => query.OrderByDescending(d => d.Status == StatusDemanda.SemResponsavel ? 0
                : d.Status == StatusDemanda.Pendente ? 1 : d.Status == StatusDemanda.EmAndamento ? 2 : d.Status == StatusDemanda.Concluido ? 3 : 4),
            _ => query.OrderBy(d => d.DataInicio),
        };

    private static IOrderedQueryable<Demanda> AplicarProximaChave(IOrderedQueryable<Demanda> query, OrdenacaoDemanda chave) =>
        (chave.Campo, chave.Descendente) switch
        {
            (CampoOrdenacaoDemanda.DataInicio, false) => query.ThenBy(d => d.DataInicio),
            (CampoOrdenacaoDemanda.DataInicio, true) => query.ThenByDescending(d => d.DataInicio),
            (CampoOrdenacaoDemanda.TempoDecorrido, false) => query.ThenByDescending(d => d.DataInicio),
            (CampoOrdenacaoDemanda.TempoDecorrido, true) => query.ThenBy(d => d.DataInicio),
            (CampoOrdenacaoDemanda.DataFimPrevista, false) => query.ThenBy(d => d.DataFimPrevista),
            (CampoOrdenacaoDemanda.DataFimPrevista, true) => query.ThenByDescending(d => d.DataFimPrevista),
            (CampoOrdenacaoDemanda.Prioridade, false) => query.ThenBy(d => d.Prioridade == Prioridade.Baixa ? 0
                : d.Prioridade == Prioridade.Media ? 1 : d.Prioridade == Prioridade.Alta ? 2 : 3),
            (CampoOrdenacaoDemanda.Prioridade, true) => query.ThenByDescending(d => d.Prioridade == Prioridade.Baixa ? 0
                : d.Prioridade == Prioridade.Media ? 1 : d.Prioridade == Prioridade.Alta ? 2 : 3),
            (CampoOrdenacaoDemanda.Status, false) => query.ThenBy(d => d.Status == StatusDemanda.SemResponsavel ? 0
                : d.Status == StatusDemanda.Pendente ? 1 : d.Status == StatusDemanda.EmAndamento ? 2 : d.Status == StatusDemanda.Concluido ? 3 : 4),
            (CampoOrdenacaoDemanda.Status, true) => query.ThenByDescending(d => d.Status == StatusDemanda.SemResponsavel ? 0
                : d.Status == StatusDemanda.Pendente ? 1 : d.Status == StatusDemanda.EmAndamento ? 2 : d.Status == StatusDemanda.Concluido ? 3 : 4),
            _ => query.ThenBy(d => d.DataInicio),
        };
}
