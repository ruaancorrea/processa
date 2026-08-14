using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed record ClienteResumo(
    Guid Id, string RazaoSocial, string Cnpj, Guid? GrupoClienteId, RegimeTributario RegimeTributario, StatusCliente Status);

public sealed record ListarClientesQuery : IRequest<List<ClienteResumo>>;

public sealed class ListarClientesQueryHandler(IClienteRepository clienteRepository)
    : IRequestHandler<ListarClientesQuery, List<ClienteResumo>>
{
    public async Task<List<ClienteResumo>> Handle(ListarClientesQuery request, CancellationToken cancellationToken)
    {
        var clientes = await clienteRepository.ListarAsync(cancellationToken);
        return clientes
            .Select(c => new ClienteResumo(c.Id, c.RazaoSocial, c.Cnpj.Numero, c.GrupoClienteId, c.RegimeTributario, c.Status))
            .ToList();
    }
}
