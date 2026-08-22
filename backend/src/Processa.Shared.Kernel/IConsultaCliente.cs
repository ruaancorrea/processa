namespace Processa.Shared.Kernel;

/// <summary>Nomes de exibição de clientes (kanban/lista de Demandas, Sprint 6) — não confundir com IVerificadorCliente (existência/ativo).</summary>
public interface IConsultaCliente
{
    Task<IReadOnlyDictionary<Guid, string>> ObterRazoesSociaisAsync(IEnumerable<Guid> clienteIds, CancellationToken ct = default);
}
