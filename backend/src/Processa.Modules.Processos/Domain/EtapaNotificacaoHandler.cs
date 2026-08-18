using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Porta pro envio real — o disparo por canal (email/whatsapp/interno) é do módulo
/// Notificações (Sprint 8). Aqui (Sprint 4) o adapter de Infrastructure só registra
/// a intenção; o handler decide QUANDO enviar e sempre avança em seguida.
/// </summary>
public interface INotificadorEtapa
{
    Task EnviarAsync(
        Guid tenantId, Guid demandaId, DestinatarioNotificacao destinatario, CanalNotificacao canal, string mensagem,
        CancellationToken cancellationToken = default);
}

/// <summary>Etapa que só notifica e segue — não espera ação humana (PROJ-48).</summary>
public sealed class EtapaNotificacaoHandler(INotificadorEtapa notificadorEtapa) : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Notificacao;

    public async Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        var configuracao = JsonSerializer.Deserialize<ConfiguracaoEtapaNotificacao>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa de notificação ausente ou inválida.");

        await notificadorEtapa.EnviarAsync(
            contexto.TenantId, contexto.DemandaId, configuracao.Destinatario, configuracao.Canal, configuracao.Mensagem, cancellationToken);

        return new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida);
    }
}
