using Microsoft.AspNetCore.SignalR;
using Processa.Modules.Processos.Application;

namespace Processa.Modules.Processos.Infrastructure;

public sealed class NotificadorKanban(IHubContext<KanbanHub> hubContext) : IKanbanNotificador
{
    public const string EventoQuadroAlterado = "quadroAlterado";

    public Task NotificarQuadroAlteradoAsync(Guid equipeId, Guid tipoProcessoId, CancellationToken ct = default) =>
        hubContext.Clients.Group(KanbanHub.NomeGrupo(equipeId)).SendAsync(EventoQuadroAlterado, tipoProcessoId, ct);
}
