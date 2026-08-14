using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;

namespace Processa.Modules.Clientes.Application.Grupos;

public sealed record GrupoClienteResumo(Guid Id, string Nome, string? Descricao);

public sealed record ListarGruposClienteQuery : IRequest<List<GrupoClienteResumo>>;

public sealed class ListarGruposClienteQueryHandler(IGrupoClienteRepository grupoClienteRepository)
    : IRequestHandler<ListarGruposClienteQuery, List<GrupoClienteResumo>>
{
    public async Task<List<GrupoClienteResumo>> Handle(ListarGruposClienteQuery request, CancellationToken cancellationToken)
    {
        var grupos = await grupoClienteRepository.ListarAsync(cancellationToken);
        return grupos.Select(g => new GrupoClienteResumo(g.Id, g.Nome, g.Descricao)).ToList();
    }
}
