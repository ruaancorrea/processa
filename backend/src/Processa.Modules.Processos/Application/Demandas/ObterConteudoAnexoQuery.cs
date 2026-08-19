using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

public sealed record ConteudoAnexo(Stream Conteudo, string NomeOriginal, string MimeType);

public sealed record ObterConteudoAnexoQuery(Guid AnexoId) : IRequest<Result<ConteudoAnexo>>;

public sealed class ObterConteudoAnexoQueryHandler(IAnexoExecucaoRepository anexoRepository, IArmazenamentoArquivo armazenamentoArquivo)
    : IRequestHandler<ObterConteudoAnexoQuery, Result<ConteudoAnexo>>
{
    public async Task<Result<ConteudoAnexo>> Handle(ObterConteudoAnexoQuery request, CancellationToken cancellationToken)
    {
        var anexo = await anexoRepository.ObterPorIdAsync(request.AnexoId, cancellationToken);
        if (anexo is null)
            return Result.Failure<ConteudoAnexo>("Anexo não encontrado.");

        var conteudo = await armazenamentoArquivo.AbrirAsync(anexo.CaminhoStorage, cancellationToken);
        return Result.Success(new ConteudoAnexo(conteudo, anexo.NomeOriginal, anexo.MimeType));
    }
}
