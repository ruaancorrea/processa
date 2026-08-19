namespace Processa.Modules.Processos.Application;

/// <summary>Porta pro storage real de arquivo (MinIO/S3, ver Infrastructure.ArmazenamentoArquivoS3) — Application não conhece bucket/credencial.</summary>
public interface IArmazenamentoArquivo
{
    Task<string> SalvarAsync(string caminho, Stream conteudo, string mimeType, CancellationToken ct = default);
    Task<Stream> AbrirAsync(string caminho, CancellationToken ct = default);
}
