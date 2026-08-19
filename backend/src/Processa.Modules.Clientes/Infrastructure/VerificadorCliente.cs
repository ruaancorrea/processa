using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure;

public sealed class VerificadorCliente(IClienteRepository clienteRepository) : IVerificadorCliente
{
    public async Task<bool> ExisteAtivoAsync(Guid clienteId, CancellationToken ct = default)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(clienteId, ct);
        return cliente is not null && cliente.Status == StatusCliente.Ativo;
    }
}
