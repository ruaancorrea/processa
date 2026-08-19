namespace Processa.Modules.Processos.Infrastructure;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    public string ServiceUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
}
