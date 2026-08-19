using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>Metadado do anexo — os bytes em si vivem no storage real (MinIO/S3, ver IArmazenamentoArquivo), não aqui.</summary>
public sealed class AnexoExecucao : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ExecucaoEtapaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string NomeOriginal { get; private set; }
    public string NomeArmazenado { get; private set; }
    public string CaminhoStorage { get; private set; }
    public long TamanhoBytes { get; private set; }
    public string MimeType { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private AnexoExecucao()
    {
        NomeOriginal = string.Empty;
        NomeArmazenado = string.Empty;
        CaminhoStorage = string.Empty;
        MimeType = string.Empty;
    }

    private AnexoExecucao(
        Guid id, Guid tenantId, Guid execucaoEtapaId, Guid usuarioId, string nomeOriginal, string nomeArmazenado,
        string caminhoStorage, long tamanhoBytes, string mimeType) : base(id)
    {
        TenantId = tenantId;
        ExecucaoEtapaId = execucaoEtapaId;
        UsuarioId = usuarioId;
        NomeOriginal = nomeOriginal;
        NomeArmazenado = nomeArmazenado;
        CaminhoStorage = caminhoStorage;
        TamanhoBytes = tamanhoBytes;
        MimeType = mimeType;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<AnexoExecucao> Criar(
        Guid tenantId, Guid execucaoEtapaId, Guid usuarioId, string? nomeOriginal, string nomeArmazenado,
        string caminhoStorage, long tamanhoBytes, string? mimeType)
    {
        if (tenantId == Guid.Empty || execucaoEtapaId == Guid.Empty || usuarioId == Guid.Empty)
            return Result.Failure<AnexoExecucao>("Tenant, execução e usuário são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nomeOriginal))
            return Result.Failure<AnexoExecucao>("O nome do arquivo é obrigatório.");

        if (string.IsNullOrWhiteSpace(nomeArmazenado) || string.IsNullOrWhiteSpace(caminhoStorage))
            return Result.Failure<AnexoExecucao>("Falha ao registrar onde o arquivo foi armazenado.");

        if (tamanhoBytes <= 0)
            return Result.Failure<AnexoExecucao>("O tamanho do arquivo deve ser maior que zero.");

        return Result.Success(new AnexoExecucao(
            Guid.NewGuid(), tenantId, execucaoEtapaId, usuarioId, nomeOriginal.Trim(), nomeArmazenado, caminhoStorage, tamanhoBytes,
            string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType));
    }
}
