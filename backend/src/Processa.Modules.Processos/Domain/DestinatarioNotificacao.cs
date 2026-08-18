namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Subconjunto do destinatario_tipo de docs/03-modelagem/modelo-de-dados.md#notificacoes_etapa
/// relevante à configuração da Etapa de Notificação (Sprint 4) — o envio de verdade
/// (canal email/whatsapp, template renderizado) é do módulo Notificações (Sprint 8).
/// </summary>
public enum DestinatarioNotificacao
{
    Responsavel,
    ContatoCliente,
    Usuario,
    Equipe,
}
