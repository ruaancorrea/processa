using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Domain;

/// <summary>
/// Um membro de equipe marcado como responsável por um cliente, naquela equipe
/// (independente do responsável de uma demanda pontual — módulo Processos).
/// EquipeId/UsuarioId são Guid puro: Clientes não pode referenciar o Domain de
/// Identidade diretamente (ver .claude/architecture.md e .faf/decisions.faf).
/// Soft-delete via <see cref="RemovidoEm"/> — permite reativar o mesmo vínculo
/// sem violar a constraint de unicidade (ver EF configuration).
/// </summary>
public sealed class ResponsavelCliente : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid EquipeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RemovidoEm { get; private set; }

    public bool EstaAtivo => RemovidoEm is null;

    private ResponsavelCliente()
    {
    }

    private ResponsavelCliente(Guid id, Guid tenantId, Guid clienteId, Guid equipeId, Guid usuarioId) : base(id)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        EquipeId = equipeId;
        UsuarioId = usuarioId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<ResponsavelCliente> Criar(Guid tenantId, Guid clienteId, Guid equipeId, Guid usuarioId)
    {
        if (tenantId == Guid.Empty || clienteId == Guid.Empty || equipeId == Guid.Empty || usuarioId == Guid.Empty)
            return Result.Failure<ResponsavelCliente>("Tenant, cliente, equipe e usuário são obrigatórios.");

        return Result.Success(new ResponsavelCliente(Guid.NewGuid(), tenantId, clienteId, equipeId, usuarioId));
    }

    public void Remover() => RemovidoEm = DateTimeOffset.UtcNow;

    public void Reativar() => RemovidoEm = null;
}
