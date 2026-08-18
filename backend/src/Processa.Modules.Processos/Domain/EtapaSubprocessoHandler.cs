using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Porta pra disparar o processo filho — a criação real de uma Demanda (com
/// herança de campos personalizados do pai) é Sprint 5; aqui o handler só decide
/// QUANDO disparar e que fica aguardando o filho concluir.
/// </summary>
public interface ICriadorSubprocesso
{
    Task<Guid> CriarAsync(
        Guid tenantId, Guid demandaPaiId, Guid tipoProcessoFilhoId, bool herdarResponsavel, CancellationToken cancellationToken = default);
}

/// <summary>Dispara um processo filho e aguarda sua conclusão antes de avançar (PROJ-49).</summary>
public sealed class EtapaSubprocessoHandler(ICriadorSubprocesso criadorSubprocesso) : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Subprocesso;

    public async Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        var configuracao = JsonSerializer.Deserialize<ConfiguracaoEtapaSubprocesso>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa de subprocesso ausente ou inválida.");

        var demandaFilhaId = await criadorSubprocesso.CriarAsync(
            contexto.TenantId, contexto.DemandaId, configuracao.TipoProcessoFilhoId, configuracao.HerdarResponsavel, cancellationToken);

        return new ResultadoExecucaoEtapa(DesfechoExecucao.Aguardando, Motivo: $"Aguardando conclusão do subprocesso {demandaFilhaId}.");
    }
}
