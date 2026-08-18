using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record TipoProcessoResumo(Guid Id, string Nome, Guid EquipeId, ModoAtribuicao ModoAtribuicao, bool Ativo);

public sealed record ListarTiposProcessoQuery : IRequest<List<TipoProcessoResumo>>;

public sealed class ListarTiposProcessoQueryHandler(ITipoProcessoRepository tipoProcessoRepository)
    : IRequestHandler<ListarTiposProcessoQuery, List<TipoProcessoResumo>>
{
    public async Task<List<TipoProcessoResumo>> Handle(ListarTiposProcessoQuery request, CancellationToken cancellationToken)
    {
        var tipos = await tipoProcessoRepository.ListarAsync(cancellationToken);
        return tipos.Select(t => new TipoProcessoResumo(t.Id, t.Nome, t.EquipeId, t.ModoAtribuicao, t.Ativo)).ToList();
    }
}
