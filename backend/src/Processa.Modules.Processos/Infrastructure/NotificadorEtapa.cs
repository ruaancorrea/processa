using Microsoft.Extensions.Logging;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Implementação real mas deliberadamente mínima: registra a intenção de notificar
/// (log estruturado), não entrega de verdade em nenhum canal externo — envio por
/// e-mail/WhatsApp é do módulo Notificações (Sprint 8). Diferente do padrão
/// "NaoImplementado" dos outros adapters: este PRECISA não lançar, porque o
/// orquestrador chama de verdade ao passar por uma Etapa de Notificação.
/// </summary>
public sealed class NotificadorEtapa(ILogger<NotificadorEtapa> logger) : INotificadorEtapa
{
    public Task EnviarAsync(
        Guid tenantId, Guid demandaId, DestinatarioNotificacao destinatario, CanalNotificacao canal, string mensagem,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Notificação de etapa (envio real pendente do módulo Notificações — Sprint 8): tenant={TenantId} demanda={DemandaId} destinatario={Destinatario} canal={Canal} mensagem={Mensagem}",
            tenantId, demandaId, destinatario, canal, mensagem);

        return Task.CompletedTask;
    }
}
