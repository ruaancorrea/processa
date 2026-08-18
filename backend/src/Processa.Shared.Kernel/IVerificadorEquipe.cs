namespace Processa.Shared.Kernel;

/// <summary>
/// Contrato de fronteira entre módulos, mesmo padrão de <see cref="IVerificadorMembroEquipe"/>
/// (Sprint 2) — Processos precisa confirmar que uma Equipe existe (TipoProcesso.EquipeId)
/// sem referenciar o Domain/Infrastructure de Identidade.
/// </summary>
public interface IVerificadorEquipe
{
    Task<bool> ExisteAsync(Guid equipeId, CancellationToken ct = default);
}
