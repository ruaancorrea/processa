using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record ColunaKanban(Guid EtapaId, string EtapaNome, int Ordem, TipoEtapa Tipo);

/// <summary>ExecucaoEtapaId é o que o frontend usa pra chamar POST /execucoes-etapa/{id}/concluir ao arrastar o card — DemandaId/EtapaAtualId sozinhos não bastam pra isso.</summary>
public sealed record CardKanban(
    Guid DemandaId, Guid ExecucaoEtapaId, Guid EtapaAtualId, Guid ClienteId, string ClienteNome, Guid? ResponsavelId, string? ResponsavelNome,
    Prioridade Prioridade, DateTimeOffset DataInicio, DateTimeOffset? DataFimPrevista);

/// <summary>
/// Board kanban (PROJ-57): coluna = Etapa do fluxo padrão do TipoProcesso, ordenada por
/// Etapa.Ordem — decisão explícita do usuário (não StatusDemanda, cujos 5 valores não
/// cobrem um exemplo como "Revisão" citado nos requisitos). Card = Demanda na coluna de
/// Demanda.EtapaAtualId. Drag-and-drop é só-avança (decisão explícita): o frontend decide
/// destino válido comparando Etapa.Ordem, não precisa de um campo "arrastável" aqui — mover
/// de verdade é a MESMA ConcluirExecucaoEtapaCommand já existente, o orquestrador decide o
/// próximo passo real (pode não ser a coluna visualmente adjacente, ex.: redirecionamento
/// por Condicional ou fork).
/// </summary>
public sealed record ObterKanbanQuery(Guid TipoProcessoId) : IRequest<Result<KanbanBoard>>;

public sealed record KanbanBoard(Guid TipoProcessoId, Guid FluxoId, IReadOnlyList<ColunaKanban> Colunas, IReadOnlyList<CardKanban> Cards);

public sealed class ObterKanbanQueryHandler(
    IFluxoRepository fluxoRepository,
    IEtapaRepository etapaRepository,
    IDemandaRepository demandaRepository,
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IConsultaCliente consultaCliente,
    IConsultaUsuario consultaUsuario) : IRequestHandler<ObterKanbanQuery, Result<KanbanBoard>>
{
    public async Task<Result<KanbanBoard>> Handle(ObterKanbanQuery request, CancellationToken ct)
    {
        var fluxo = await fluxoRepository.ObterPadraoAsync(request.TipoProcessoId, ct);
        if (fluxo is null)
            return Result.Failure<KanbanBoard>("Tipo de processo não tem um fluxo padrão configurado.");

        var etapas = await etapaRepository.ListarPorFluxoAsync(fluxo.Id, ct);
        var etapaIdsDoFluxo = etapas.Select(e => e.Id).ToHashSet();

        var candidatas = await demandaRepository.ListarParaKanbanAsync(request.TipoProcessoId, ct);
        var demandas = candidatas.Where(d => d.EtapaAtualId.HasValue && etapaIdsDoFluxo.Contains(d.EtapaAtualId.Value)).ToList();

        var clienteIds = demandas.Select(d => d.ClienteId).Distinct().ToList();
        var responsavelIds = demandas.Where(d => d.ResponsavelId.HasValue).Select(d => d.ResponsavelId!.Value).Distinct().ToList();
        var clientes = await consultaCliente.ObterRazoesSociaisAsync(clienteIds, ct);
        var responsaveis = await consultaUsuario.ObterNomesAsync(responsavelIds, ct);
        var execucoes = await execucaoEtapaRepository.ListarPorDemandaIdsAsync(demandas.Select(d => d.Id), ct);
        var execucaoPorDemandaEEtapa = execucoes.ToDictionary(e => (e.DemandaId, e.EtapaId), e => e.Id);

        var colunas = etapas.Select(e => new ColunaKanban(e.Id, e.Nome, e.Ordem, e.Tipo)).ToList();
        var cards = demandas
            .Where(d => execucaoPorDemandaEEtapa.ContainsKey((d.Id, d.EtapaAtualId!.Value)))
            .Select(d => new CardKanban(
                d.Id, execucaoPorDemandaEEtapa[(d.Id, d.EtapaAtualId!.Value)], d.EtapaAtualId!.Value,
                d.ClienteId, clientes.GetValueOrDefault(d.ClienteId, "—"),
                d.ResponsavelId, d.ResponsavelId is { } responsavelId ? responsaveis.GetValueOrDefault(responsavelId, "—") : null,
                d.Prioridade, d.DataInicio, d.DataFimPrevista))
            .ToList();

        return Result.Success(new KanbanBoard(request.TipoProcessoId, fluxo.Id, colunas, cards));
    }
}
