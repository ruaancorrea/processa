namespace Processa.Modules.Processos.Application;

/// <summary>
/// Porta pro push em tempo real do board kanban (PROJ-60, ADR-009) — Application não
/// conhece SignalR/Hub. Sinal de "invalidar e buscar de novo", não um payload granular:
/// o frontend, ao receber, só re-busca ObterKanbanQuery — mais simples e robusto que
/// tentar manter estado parcial client-side em sincronia com cada tipo de mudança
/// (movimentação de card OU troca de responsável disparam o mesmo sinal).
/// </summary>
public interface IKanbanNotificador
{
    Task NotificarQuadroAlteradoAsync(Guid equipeId, Guid tipoProcessoId, CancellationToken ct = default);
}
