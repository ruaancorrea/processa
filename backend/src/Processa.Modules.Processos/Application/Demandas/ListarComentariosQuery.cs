using MediatR;
using Processa.Modules.Processos.Application.Repositorios;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record ComentarioResumo(Guid Id, Guid UsuarioId, string Texto, DateTimeOffset CreatedAt);

public sealed record ListarComentariosQuery(Guid ExecucaoEtapaId) : IRequest<List<ComentarioResumo>>;

public sealed class ListarComentariosQueryHandler(IComentarioExecucaoRepository comentarioRepository)
    : IRequestHandler<ListarComentariosQuery, List<ComentarioResumo>>
{
    public async Task<List<ComentarioResumo>> Handle(ListarComentariosQuery request, CancellationToken cancellationToken)
    {
        var comentarios = await comentarioRepository.ListarPorExecucaoAsync(request.ExecucaoEtapaId, cancellationToken);
        return comentarios.Select(c => new ComentarioResumo(c.Id, c.UsuarioId, c.Texto, c.CreatedAt)).ToList();
    }
}
