using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// A União só avança quando o número de desdobramentos registrados bate com o
/// configurado (EtapasAguardadasIds) — checar só "todos os que existem estão
/// concluídos" seria insuficiente, porque o Orquestrador cria cada
/// DesdobramentoAguardado JÁ concluído (só cria quando o ramo correspondente
/// termina) — teria dado falso positivo com apenas o primeiro ramo pronto.
/// </summary>
public sealed class VerificadorDesdobramentos(
    IExecucaoEtapaRepository execucaoEtapaRepository, IEtapaRepository etapaRepository, IDesdobramentoAguardadoRepository desdobramentoRepository)
    : IVerificadorDesdobramentos
{
    public async Task<bool> TodosConcluidosAsync(Guid tenantId, Guid execucaoEtapaUniaoId, CancellationToken cancellationToken = default)
    {
        var execucaoUniao = await execucaoEtapaRepository.ObterPorIdAsync(execucaoEtapaUniaoId, cancellationToken);
        if (execucaoUniao is null)
            return false;

        var etapaUniao = await etapaRepository.ObterPorIdAsync(execucaoUniao.EtapaId, cancellationToken);
        if (etapaUniao?.Configuracao is not ConfiguracaoEtapaUniao configuracao)
            return false;

        var desdobramentos = await desdobramentoRepository.ListarPorExecucaoUniaoAsync(execucaoEtapaUniaoId, cancellationToken);

        return desdobramentos.Count >= configuracao.EtapasAguardadasIds.Count && desdobramentos.All(d => d.Concluido);
    }
}
