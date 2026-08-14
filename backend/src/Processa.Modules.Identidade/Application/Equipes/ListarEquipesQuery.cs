using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record EquipeResumo(Guid Id, string Nome, string? Descricao, bool Ativa);

public sealed record ListarEquipesQuery : IRequest<List<EquipeResumo>>;

public sealed class ListarEquipesQueryHandler(IEquipeRepository equipeRepository)
    : IRequestHandler<ListarEquipesQuery, List<EquipeResumo>>
{
    public async Task<List<EquipeResumo>> Handle(ListarEquipesQuery request, CancellationToken cancellationToken)
    {
        var equipes = await equipeRepository.ListarAsync(cancellationToken);
        return equipes.Select(e => new EquipeResumo(e.Id, e.Nome, e.Descricao, e.Ativa)).ToList();
    }
}
