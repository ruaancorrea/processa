using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record DemandaResumo(
    Guid Id, Guid TipoProcessoId, string TipoProcessoNome, Guid ClienteId, string ClienteNome,
    Guid? ResponsavelId, string? ResponsavelNome, Guid? EtapaAtualId, string? EtapaAtualNome,
    StatusDemanda Status, Prioridade Prioridade, int PercentualConclusao,
    DateTimeOffset DataInicio, DateTimeOffset? DataFimPrevista);

/// <summary>
/// Lista/painel operacional (PROJ-58): filtro + ordenação cumulativa + paginação, com
/// nomes já resolvidos (evita N+1 no frontend). Requisitos, módulo 7.2: Gestor/Admin
/// veem a fila do time inteiro (sem-responsável primeiro por padrão); Analista só vê a
/// própria fila — imposto aqui no handler, não confia no filtro que o cliente pediu.
/// </summary>
public sealed record ListarDemandasQuery(
    FiltroDemandas? Filtro = null, IReadOnlyList<OrdenacaoDemanda>? Ordenacao = null, int Pagina = 1, int TamanhoPagina = 50)
    : IRequest<ResultadoPaginado<DemandaResumo>>;

public sealed class ListarDemandasQueryHandler(
    IDemandaRepository demandaRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    IEtapaRepository etapaRepository,
    IConsultaCliente consultaCliente,
    IConsultaUsuario consultaUsuario,
    IUsuarioContext usuarioContext) : IRequestHandler<ListarDemandasQuery, ResultadoPaginado<DemandaResumo>>
{
    public async Task<ResultadoPaginado<DemandaResumo>> Handle(ListarDemandasQuery request, CancellationToken ct)
    {
        var filtro = request.Filtro ?? new FiltroDemandas();
        if (usuarioContext.Perfil == Perfil.Analista)
            filtro = filtro with { ResponsavelId = usuarioContext.UsuarioId, SemResponsavel = false };

        var pagina = await demandaRepository.ListarComFiltroAsync(
            filtro, request.Ordenacao ?? [], request.Pagina, request.TamanhoPagina, ct);

        var tipoProcessoIds = pagina.Itens.Select(d => d.TipoProcessoId).Distinct().ToList();
        var etapaIds = pagina.Itens.Where(d => d.EtapaAtualId.HasValue).Select(d => d.EtapaAtualId!.Value).Distinct().ToList();
        var clienteIds = pagina.Itens.Select(d => d.ClienteId).Distinct().ToList();
        var responsavelIds = pagina.Itens.Where(d => d.ResponsavelId.HasValue).Select(d => d.ResponsavelId!.Value).Distinct().ToList();

        var tiposProcesso = (await tipoProcessoRepository.ListarPorIdsAsync(tipoProcessoIds, ct)).ToDictionary(t => t.Id, t => t.Nome);
        var etapas = (await etapaRepository.ListarPorIdsAsync(etapaIds, ct)).ToDictionary(e => e.Id, e => e.Nome);
        var clientes = await consultaCliente.ObterRazoesSociaisAsync(clienteIds, ct);
        var responsaveis = await consultaUsuario.ObterNomesAsync(responsavelIds, ct);

        var itens = pagina.Itens.Select(d => new DemandaResumo(
            d.Id, d.TipoProcessoId, tiposProcesso.GetValueOrDefault(d.TipoProcessoId, "—"),
            d.ClienteId, clientes.GetValueOrDefault(d.ClienteId, "—"),
            d.ResponsavelId, d.ResponsavelId is { } responsavelId ? responsaveis.GetValueOrDefault(responsavelId, "—") : null,
            d.EtapaAtualId, d.EtapaAtualId is { } etapaId ? etapas.GetValueOrDefault(etapaId, "—") : null,
            d.Status, d.Prioridade, d.PercentualConclusao, d.DataInicio, d.DataFimPrevista)).ToList();

        return new ResultadoPaginado<DemandaResumo>(itens, pagina.TotalRegistros, pagina.Pagina, pagina.TamanhoPagina);
    }
}
