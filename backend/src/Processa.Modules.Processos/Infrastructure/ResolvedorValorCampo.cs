using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Busca o valor mais recente de um campo personalizado entre TODAS as ExecucaoEtapa
/// da demanda — os valores preenchidos no formulário de abertura ficam na primeira
/// execução criada; etapas seguintes podem sobrescrever (CriarDemandaCommand grava lá).
/// </summary>
public sealed class ResolvedorValorCampo(IExecucaoEtapaRepository execucaoEtapaRepository) : IResolvedorValorCampo
{
    public async Task<string?> ObterValorAsync(Guid tenantId, Guid demandaId, Guid campoPersonalizadoId, CancellationToken cancellationToken = default)
    {
        var execucoes = await execucaoEtapaRepository.ListarPorDemandaAsync(demandaId, cancellationToken);
        var chave = campoPersonalizadoId.ToString();

        return execucoes
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => e.DadosExecucao.TryGetValue(chave, out var valor) ? valor : null)
            .FirstOrDefault(v => v is not null);
    }
}
