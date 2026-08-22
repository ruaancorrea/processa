using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// ADR-009: grupo por equipe_id (não por tipo de processo — mais de um TipoProcesso pode
/// pertencer à mesma equipe, e o board de qualquer um deles interessa a quem gerencia a
/// equipe). O cliente entra/sai do grupo explicitamente ao abrir/fechar um board específico
/// (não entra em todas as equipes do usuário na conexão) — evita tráfego de board que
/// ninguém está olhando. IVerificadorEquipe.ExisteAsync já filtra por tenant (mesmo
/// Global Query Filter de todo o resto) — impede entrar no grupo de uma equipe de outro
/// tenant; NÃO impede entrar no grupo de outra equipe do MESMO tenant (mesma simplificação
/// já aceita de RBAC sem isolamento por equipe, ver .faf/pendencias.faf).
/// </summary>
[Authorize]
public sealed class KanbanHub(IVerificadorEquipe verificadorEquipe) : Hub
{
    public async Task EntrarGrupoEquipe(Guid equipeId)
    {
        if (await verificadorEquipe.ExisteAsync(equipeId))
            await Groups.AddToGroupAsync(Context.ConnectionId, NomeGrupo(equipeId));
    }

    public async Task SairGrupoEquipe(Guid equipeId) =>
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, NomeGrupo(equipeId));

    public static string NomeGrupo(Guid equipeId) => $"equipe:{equipeId}";
}
