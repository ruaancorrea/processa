using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Processa.Modules.Processos.Application;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>
/// Storage real via AWSSDK.S3 apontado pro MinIO local (ServiceURL customizado —
/// funciona com qualquer endpoint S3-compatível, não só MinIO). Cria o bucket na
/// primeira chamada se ele ainda não existir (dev/CI não têm provisionamento
/// separado de infraestrutura de bucket). Registrada como Singleton (DI) — mesma
/// recomendação da própria AWS pra AmazonS3Client (thread-safe, caro de construir,
/// não deve ser recriado por request); também é o que permite o cache abaixo
/// funcionar de verdade (uma instância Scoped seria recriada e perderia o cache
/// a cada requisição, batendo em ListBucketsAsync sempre).
/// </summary>
public sealed class ArmazenamentoArquivoS3 : IArmazenamentoArquivo
{
    private readonly IAmazonS3 _cliente;
    private readonly string _bucket;
    private volatile bool _bucketConfirmado;

    public ArmazenamentoArquivoS3(IOptions<MinioOptions> options)
    {
        var config = options.Value;
        _bucket = config.Bucket;
        _cliente = new AmazonS3Client(
            config.AccessKey,
            config.SecretKey,
            new AmazonS3Config { ServiceURL = config.ServiceUrl, ForcePathStyle = true, UseHttp = config.ServiceUrl.StartsWith("http://") });
    }

    public async Task<string> SalvarAsync(string caminho, Stream conteudo, string mimeType, CancellationToken ct = default)
    {
        await GarantirBucketAsync(ct);

        await _cliente.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = caminho,
            InputStream = conteudo,
            ContentType = mimeType,
            AutoCloseStream = false,
        }, ct);

        return caminho;
    }

    public async Task<Stream> AbrirAsync(string caminho, CancellationToken ct = default)
    {
        var resposta = await _cliente.GetObjectAsync(new GetObjectRequest { BucketName = _bucket, Key = caminho }, ct);
        return resposta.ResponseStream;
    }

    private async Task GarantirBucketAsync(CancellationToken ct)
    {
        // Corrida benigna possível (duas requisições confirmando o bucket ao mesmo
        // tempo antes de qualquer uma setar a flag): na pior hipótese, ListBuckets
        // roda 2x em vez de 1x nesse instante — nunca cria o bucket em duplicidade
        // (PutBucketAsync pra um bucket já existente é idempotente no S3/MinIO).
        if (_bucketConfirmado)
            return;

        var buckets = await _cliente.ListBucketsAsync(ct);
        // AWSSDK.S3 v4 contra MinIO: Buckets vem null (não uma lista vazia) quando a conta
        // ainda não tem nenhum bucket — resposta XML sem o elemento, não um elemento vazio.
        if (buckets.Buckets?.Any(b => b.BucketName == _bucket) != true)
            await _cliente.PutBucketAsync(new PutBucketRequest { BucketName = _bucket }, ct);

        _bucketConfirmado = true;
    }
}
