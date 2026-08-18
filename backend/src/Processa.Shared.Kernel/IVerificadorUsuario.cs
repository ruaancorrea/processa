namespace Processa.Shared.Kernel;

/// <summary>
/// Contrato de fronteira entre módulos, mesmo padrão de <see cref="IVerificadorMembroEquipe"/>
/// (Sprint 2) — Processos precisa confirmar que um Usuario existe (responsável fixo,
/// permissões de início por usuário) sem referenciar o Domain/Infrastructure de Identidade.
/// </summary>
public interface IVerificadorUsuario
{
    Task<bool> ExisteAsync(Guid usuarioId, CancellationToken ct = default);
}
