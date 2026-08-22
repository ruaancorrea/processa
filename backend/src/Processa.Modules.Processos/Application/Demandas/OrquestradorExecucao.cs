using System.Text.Json;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>
/// O motor de verdade (ver ADR-003): dado uma Demanda, avança etapas em ordem linear
/// dentro do FluxoAtivo, exceto quando uma Condicional redireciona (um ramo casa) ou
/// dispara um fork real (mais de um ramo casa ao mesmo tempo — múltiplas ExecucaoEtapa
/// da mesma Demanda ficam ativas em paralelo). Cada ramo avança sozinho até parar numa
/// Comum (espera ação humana), numa Etapa aguardando algo (Agendamento não vencido,
/// União incompleta, Subprocesso), ou convergir numa União.
///
/// União: quando qualquer Condicional conclui, verificamos se alguma etapa União do
/// mesmo Fluxo lista essa Condicional em EtapasAguardadasIds — se sim, criamos (ou
/// reaproveitamos) a ExecucaoEtapa da União antecipadamente e registramos um
/// DesdobramentoAguardado concluído pra esse ramo. Isso resolve a corrida em que um
/// ramo mais rápido chegaria na União antes do outro ramo sequer ter concluído sua
/// Condicional: a União só é realmente avaliada (via IVerificadorDesdobramentos, que
/// olha os DesdobramentoAguardado) quando ela é fisicamente alcançada por navegação
/// OU quando o último ramo pendente é registrado — reavaliamos nos dois casos.
/// </summary>
public sealed class OrquestradorExecucao(
    IEtapaRepository etapaRepository,
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IDesdobramentoAguardadoRepository desdobramentoRepository,
    IDemandaRepository demandaRepository,
    ITipoProcessoRepository tipoProcessoRepository,
    EtapaHandlerFactory etapaHandlerFactory,
    IKanbanNotificador kanbanNotificador,
    IUnitOfWork unitOfWork)
{
    public async Task IniciarAsync(Demanda demanda, CancellationToken ct = default)
    {
        var etapas = await etapaRepository.ListarPorFluxoAsync(demanda.FluxoAtivoId, ct);
        var primeira = etapas.OrderBy(e => e.Ordem).FirstOrDefault();
        if (primeira is null)
            return;

        await AvancarRamoAsync(demanda, primeira, ct);
        await NotificarKanbanAsync(demanda, ct);
    }

    /// <summary>Chamado quando o responsável conclui manualmente uma etapa Comum, ou quando um subprocesso filho concluiu e o ramo pai (Aguardando) pode seguir.</summary>
    public async Task ContinuarAposConclusaoManualAsync(Demanda demanda, ExecucaoEtapa execucaoConcluida, CancellationToken ct = default)
    {
        var etapaConcluida = await etapaRepository.ObterPorIdAsync(execucaoConcluida.EtapaId, ct);
        if (etapaConcluida is null)
            return;

        await ProcessarConclusaoAsync(demanda, etapaConcluida, execucaoConcluida, null, ct);
        await NotificarKanbanAsync(demanda, ct);
    }

    /// <summary>
    /// Chamado só nos pontos de entrada públicos (não a cada AvancarRamoAsync interno),
    /// depois que toda a cascata recursiva (fork, União, etc.) já foi persistida — nunca
    /// avisa o frontend pra buscar dado que ainda não commitou. Silencioso se o
    /// TipoProcesso não existir mais (não deveria acontecer, mas notificação não é
    /// motivo pra falhar a operação principal).
    /// </summary>
    private async Task NotificarKanbanAsync(Demanda demanda, CancellationToken ct)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(demanda.TipoProcessoId, ct);
        if (tipoProcesso is not null)
            await kanbanNotificador.NotificarQuadroAlteradoAsync(tipoProcesso.EquipeId, demanda.TipoProcessoId, ct);
    }

    private async Task AvancarRamoAsync(Demanda demanda, Etapa etapa, CancellationToken ct)
    {
        var execucao = await execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapa.Id, ct);
        var reaproveitada = execucao is not null;
        if (execucao is null)
        {
            execucao = ExecucaoEtapa.Criar(demanda.TenantId, demanda.Id, etapa.Id, demanda.ResponsavelId).Value;
            await execucaoEtapaRepository.AddAsync(execucao, ct);
        }

        // Etapa não-União já processada por outro ramo (convergência acidental fora de
        // União) — não reprocessa. União sempre reavalia: outro ramo pode ter acabado
        // de completar o conjunto aguardado.
        if (reaproveitada && etapa.Tipo != TipoEtapa.Uniao && execucao.Status != StatusExecucaoEtapa.Pendente)
            return;

        demanda.IniciarEtapa(etapa.Id);
        execucao.Iniciar();

        if (etapa.Tipo == TipoEtapa.Comum)
        {
            await unitOfWork.SalvarAsync(ct);
            return;
        }

        var handler = etapaHandlerFactory.ObterHandler(etapa.Tipo);
        var configuracaoJson = etapa.Configuracao is null ? "{}" : JsonSerializer.Serialize(etapa.Configuracao);
        var contexto = new ExecucaoEtapaContexto(demanda.TenantId, demanda.Id, execucao.Id, configuracaoJson, execucao.IniciadoEm);
        var resultado = await handler.ExecutarAsync(contexto, ct);

        await ProcessarResultadoAsync(demanda, etapa, execucao, resultado, ct);
    }

    private async Task ProcessarResultadoAsync(
        Demanda demanda, Etapa etapa, ExecucaoEtapa execucao, ResultadoExecucaoEtapa resultado, CancellationToken ct)
    {
        switch (resultado.Desfecho)
        {
            case DesfechoExecucao.Concluida:
                execucao.Concluir();
                await unitOfWork.SalvarAsync(ct);
                await ProcessarConclusaoAsync(demanda, etapa, execucao, resultado.ProximasEtapasIds, ct);
                return;

            case DesfechoExecucao.Aguardando:
                execucao.MarcarAguardando();
                foreach (var (chave, valor) in resultado.DadosResultantes ?? new Dictionary<string, string>())
                    execucao.DefinirDado(chave, valor);
                await unitOfWork.SalvarAsync(ct);
                return;

            case DesfechoExecucao.Travada:
                execucao.RegistrarErro();
                await unitOfWork.SalvarAsync(ct);
                return;
        }
    }

    private async Task ProcessarConclusaoAsync(
        Demanda demanda, Etapa etapaConcluida, ExecucaoEtapa execucaoConcluida, IReadOnlyList<Guid>? destinosForcados, CancellationToken ct)
    {
        // Qualquer etapa pode ser o "ramo aguardado" configurado numa União — não só
        // a Condicional que o originou (ela conclui quase instantaneamente ao decidir
        // o fork; quem realmente representa "esse ramo terminou" é a etapa no fim de
        // cada branch, tipicamente listada em ConfiguracaoEtapaUniao.EtapasAguardadasIds).
        await PropagarParaUnioesQueAguardamAsync(demanda, etapaConcluida, execucaoConcluida, ct);

        if (etapaConcluida.Tipo == TipoEtapa.Conclusao)
        {
            await FinalizarSeNaoHouverRamoAbertoAsync(demanda, ct);
            return;
        }

        // Uma etapa que é ramo aguardado de alguma União é uma PONTA de fork — não
        // segue a ordem linear do Fluxo sozinha (isso a levaria pro ramo IRMÃO, não
        // pra frente de verdade). Só avança depois via a própria União, quando/se o
        // conjunto de ramos fechar (PropagarParaUnioesQueAguardamAsync já cuidou disso
        // acima). Etapas fora de fork continuam a ordem linear normalmente.
        var ehPontaDeFork = destinosForcados is null or { Count: 0 } && await AlgumaUniaoAguardaAsync(demanda.FluxoAtivoId, etapaConcluida.Id, ct);

        var destinos = destinosForcados is { Count: > 0 }
            ? destinosForcados
            : ehPontaDeFork
                ? []
                : await ObterProximaEtapaLinearAsync(demanda.FluxoAtivoId, etapaConcluida.Id, ct) is { } proxima
                    ? [proxima.Id]
                    : [];

        foreach (var destinoId in destinos)
        {
            var proximaEtapa = await etapaRepository.ObterPorIdAsync(destinoId, ct);
            if (proximaEtapa is not null)
                await AvancarRamoAsync(demanda, proximaEtapa, ct);
        }

        // Sempre reavalia, mesmo numa ponta de fork sem próximo destino — é idempotente
        // (só conclui a Demanda se NENHUMA ExecucaoEtapa continuar aberta) e é o único
        // jeito de garantir que a Demanda feche quando o último ramo termina sem passar
        // por uma União (ex.: um ramo mal configurado que não alimenta nenhuma União).
        if (destinos.Count == 0)
            await FinalizarSeNaoHouverRamoAbertoAsync(demanda, ct);
    }

    private async Task<bool> AlgumaUniaoAguardaAsync(Guid fluxoId, Guid etapaId, CancellationToken ct)
    {
        var etapas = await etapaRepository.ListarPorFluxoAsync(fluxoId, ct);
        return etapas.Any(e => e.Tipo == TipoEtapa.Uniao
            && e.Configuracao is ConfiguracaoEtapaUniao config
            && config.EtapasAguardadasIds.Contains(etapaId));
    }

    /// <summary>
    /// Uma etapa concluiu — se ela é um dos ramos aguardados de alguma União do mesmo
    /// Fluxo (ConfiguracaoEtapaUniao.EtapasAguardadasIds), registra o desdobramento
    /// concluído e, se a União já estiver com uma execução aguardando, reavalia (o
    /// conjunto que faltava pode ter acabado de fechar). O campo
    /// DesdobramentoAguardado.ExecucaoEtapaCondicionalId segue o nome do schema
    /// documentado (modelo-de-dados.md) mesmo guardando a execução do RAMO aguardado,
    /// não necessariamente uma etapa do tipo Condicional em si.
    /// </summary>
    private async Task PropagarParaUnioesQueAguardamAsync(Demanda demanda, Etapa etapaRamo, ExecucaoEtapa execucaoRamo, CancellationToken ct)
    {
        var etapas = await etapaRepository.ListarPorFluxoAsync(demanda.FluxoAtivoId, ct);
        var unioes = etapas.Where(e => e.Tipo == TipoEtapa.Uniao
            && e.Configuracao is ConfiguracaoEtapaUniao config
            && config.EtapasAguardadasIds.Contains(etapaRamo.Id));

        foreach (var etapaUniao in unioes)
        {
            var execucaoUniao = await execucaoEtapaRepository.ObterPorDemandaEEtapaAsync(demanda.Id, etapaUniao.Id, ct);
            if (execucaoUniao is null)
            {
                execucaoUniao = ExecucaoEtapa.Criar(demanda.TenantId, demanda.Id, etapaUniao.Id, demanda.ResponsavelId).Value;
                await execucaoEtapaRepository.AddAsync(execucaoUniao, ct);
            }

            var desdobramentoExistente = await desdobramentoRepository.ObterPorExecucaoCondicionalAsync(execucaoRamo.Id, ct);
            if (desdobramentoExistente is null)
            {
                var desdobramento = DesdobramentoAguardado.Criar(demanda.TenantId, execucaoUniao.Id, execucaoRamo.Id).Value;
                desdobramento.MarcarConcluido();
                await desdobramentoRepository.AddAsync(desdobramento, ct);
            }

            await unitOfWork.SalvarAsync(ct);

            // Sempre reavalia (não só quando já está Aguardando) — uma União criada
            // aqui antecipadamente (antes de qualquer ramo navegar até ela de verdade)
            // fica em Pendente até ser processada uma vez; sem reavaliar agora ela
            // nunca sairia desse estado. AvancarRamoAsync já trata União como sempre
            // reprocessável (não é bloqueada pelo guard de "já processada").
            await AvancarRamoAsync(demanda, etapaUniao, ct);
        }
    }

    private async Task<Etapa?> ObterProximaEtapaLinearAsync(Guid fluxoId, Guid etapaAtualId, CancellationToken ct)
    {
        var etapas = (await etapaRepository.ListarPorFluxoAsync(fluxoId, ct)).OrderBy(e => e.Ordem).ToList();
        var indice = etapas.FindIndex(e => e.Id == etapaAtualId);
        return indice >= 0 && indice + 1 < etapas.Count ? etapas[indice + 1] : null;
    }

    private async Task FinalizarSeNaoHouverRamoAbertoAsync(Demanda demanda, CancellationToken ct)
    {
        var execucoes = await execucaoEtapaRepository.ListarPorDemandaAsync(demanda.Id, ct);
        var aindaAbertas = execucoes.Any(e =>
            e.Status is StatusExecucaoEtapa.Pendente or StatusExecucaoEtapa.EmAndamento or StatusExecucaoEtapa.Aguardando);

        if (aindaAbertas)
            return;

        if (!demanda.Concluir().IsSuccess)
            return;

        await unitOfWork.SalvarAsync(ct);

        if (demanda.DemandaPaiId is { } demandaPaiId)
            await RetomarRamoDoSubprocessoPaiAsync(demandaPaiId, demanda.Id, ct);
    }

    /// <summary>Demanda filha (de uma Etapa de Subprocesso) concluiu — encontra o ramo do pai que ficou Aguardando por ela e retoma.</summary>
    private async Task RetomarRamoDoSubprocessoPaiAsync(Guid demandaPaiId, Guid demandaFilhaId, CancellationToken ct)
    {
        var demandaPai = await demandaRepository.ObterPorIdAsync(demandaPaiId, ct);
        if (demandaPai is null)
            return;

        var execucoesPai = await execucaoEtapaRepository.ListarPorDemandaAsync(demandaPaiId, ct);
        var execucaoAguardando = execucoesPai.FirstOrDefault(e =>
            e.Status == StatusExecucaoEtapa.Aguardando
            && e.DadosExecucao.TryGetValue(EtapaSubprocessoHandler.DemandaFilhaChave, out var filhaId)
            && filhaId == demandaFilhaId.ToString());

        if (execucaoAguardando is null)
            return;

        if (execucaoAguardando.Concluir().IsFailure)
            return;

        await unitOfWork.SalvarAsync(ct);
        await ContinuarAposConclusaoManualAsync(demandaPai, execucaoAguardando, ct);
    }
}
