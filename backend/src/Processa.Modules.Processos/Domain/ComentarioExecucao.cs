using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

public sealed class ComentarioExecucao : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ExecucaoEtapaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Texto { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private ComentarioExecucao()
    {
        Texto = string.Empty;
    }

    private ComentarioExecucao(Guid id, Guid tenantId, Guid execucaoEtapaId, Guid usuarioId, string texto) : base(id)
    {
        TenantId = tenantId;
        ExecucaoEtapaId = execucaoEtapaId;
        UsuarioId = usuarioId;
        Texto = texto;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<ComentarioExecucao> Criar(Guid tenantId, Guid execucaoEtapaId, Guid usuarioId, string? texto)
    {
        if (tenantId == Guid.Empty || execucaoEtapaId == Guid.Empty || usuarioId == Guid.Empty)
            return Result.Failure<ComentarioExecucao>("Tenant, execução e usuário são obrigatórios.");

        if (string.IsNullOrWhiteSpace(texto))
            return Result.Failure<ComentarioExecucao>("O comentário não pode ser vazio.");

        return Result.Success(new ComentarioExecucao(Guid.NewGuid(), tenantId, execucaoEtapaId, usuarioId, texto.Trim()));
    }
}
