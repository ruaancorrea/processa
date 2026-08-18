using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Adapter deliberadamente não implementado — envio real por canal (email/whatsapp)
/// é do módulo Notificações (Sprint 8). Ver ResolvedorValorCampoNaoImplementado
/// pra justificativa de falhar alto em vez de no-op silencioso.
/// </summary>
public sealed class NotificadorEtapaNaoImplementado : INotificadorEtapa
{
    public Task EnviarAsync(
        Guid tenantId, Guid demandaId, DestinatarioNotificacao destinatario, CanalNotificacao canal, string mensagem,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException("Envio real de notificação de etapa é escopo do módulo Notificações (Sprint 8).");
}
