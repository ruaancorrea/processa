using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>Campos ordenáveis da lista/kanban de Demandas (requisitos, módulo 7.2). TempoDecorrido é DataInicio com o sentido invertido (mais tempo decorrido = data de início mais antiga).</summary>
public enum CampoOrdenacaoDemanda
{
    DataInicio,
    DataFimPrevista,
    Prioridade,
    Status,
    TempoDecorrido,
}

/// <summary>Uma chave de ordenação cumulativa — várias, aplicadas em sequência (ex.: Prioridade desc, depois DataInicio asc), formam o critério final.</summary>
public sealed record OrdenacaoDemanda(CampoOrdenacaoDemanda Campo, bool Descendente = false);

/// <summary>
/// Filtro por CNPJ/grupo de cliente não é um parâmetro aqui: resolvido pelo chamador
/// pra um ClienteId antes (Processos não conhece Cnpj/GrupoCliente, ver ADR-001).
/// Filtro por campo personalizado não implementado nesta sprint — ver .faf/pendencias.faf.
/// </summary>
public sealed record FiltroDemandas(
    StatusDemanda? Status = null,
    Guid? ResponsavelId = null,
    bool? SemResponsavel = null,
    Prioridade? Prioridade = null,
    Guid? ClienteId = null,
    Guid? TipoProcessoId = null,
    Guid? EtapaAtualId = null,
    DateTimeOffset? DataInicioDe = null,
    DateTimeOffset? DataInicioAte = null,
    DateTimeOffset? DataFimPrevistaDe = null,
    DateTimeOffset? DataFimPrevistaAte = null);

public sealed record ResultadoPaginado<T>(IReadOnlyList<T> Itens, int TotalRegistros, int Pagina, int TamanhoPagina)
{
    public int TotalPaginas => TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
}
