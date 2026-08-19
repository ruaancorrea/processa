using MediatR;
using Processa.Modules.Processos.Application.Repositorios;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record AnexoResumo(Guid Id, Guid UsuarioId, string NomeOriginal, long TamanhoBytes, string MimeType, DateTimeOffset CreatedAt);

public sealed record ListarAnexosQuery(Guid ExecucaoEtapaId) : IRequest<List<AnexoResumo>>;

public sealed class ListarAnexosQueryHandler(IAnexoExecucaoRepository anexoRepository) : IRequestHandler<ListarAnexosQuery, List<AnexoResumo>>
{
    public async Task<List<AnexoResumo>> Handle(ListarAnexosQuery request, CancellationToken cancellationToken)
    {
        var anexos = await anexoRepository.ListarPorExecucaoAsync(request.ExecucaoEtapaId, cancellationToken);
        return anexos.Select(a => new AnexoResumo(a.Id, a.UsuarioId, a.NomeOriginal, a.TamanhoBytes, a.MimeType, a.CreatedAt)).ToList();
    }
}
