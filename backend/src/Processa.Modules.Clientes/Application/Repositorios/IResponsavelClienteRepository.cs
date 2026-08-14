using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Application.Repositorios;

public interface IResponsavelClienteRepository
{
    Task AddAsync(ResponsavelCliente responsavel, CancellationToken ct = default);

    Task<ResponsavelCliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Inclui removidos — necessário pra reativar o mesmo vínculo (soft-delete) em vez de duplicar.</summary>
    Task<ResponsavelCliente?> ObterAsync(Guid clienteId, Guid equipeId, Guid usuarioId, CancellationToken ct = default);

    Task<List<ResponsavelCliente>> ListarAtivosPorClienteAsync(Guid clienteId, CancellationToken ct = default);
}
