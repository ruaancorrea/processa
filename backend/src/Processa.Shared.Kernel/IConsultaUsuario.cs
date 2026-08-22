namespace Processa.Shared.Kernel;

/// <summary>Nomes de exibição de usuários (kanban/lista de Demandas, Sprint 6) — não confundir com IVerificadorUsuario (existência).</summary>
public interface IConsultaUsuario
{
    Task<IReadOnlyDictionary<Guid, string>> ObterNomesAsync(IEnumerable<Guid> usuarioIds, CancellationToken ct = default);
}
