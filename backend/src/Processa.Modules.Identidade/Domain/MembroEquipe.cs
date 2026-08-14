using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// Vínculo usuário-equipe com papel próprio da equipe — um usuário pode
/// pertencer a múltiplas equipes com papéis diferentes em cada uma, distinto
/// do <see cref="Usuario.Perfil"/> global usado pelo RBAC de API. Só Gestor/
/// Analista fazem sentido aqui: Admin já opera o tenant inteiro (ver Sprint 1),
/// não precisa de escopo por equipe.
/// </summary>
public sealed class MembroEquipe : Entity
{
    public Guid TenantId { get; private set; }
    public Guid EquipeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Perfil Papel { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private MembroEquipe()
    {
    }

    private MembroEquipe(Guid id, Guid tenantId, Guid equipeId, Guid usuarioId, Perfil papel) : base(id)
    {
        TenantId = tenantId;
        EquipeId = equipeId;
        UsuarioId = usuarioId;
        Papel = papel;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<MembroEquipe> Adicionar(Guid tenantId, Guid equipeId, Guid usuarioId, Perfil papel)
    {
        if (tenantId == Guid.Empty || equipeId == Guid.Empty || usuarioId == Guid.Empty)
            return Result.Failure<MembroEquipe>("Tenant, equipe e usuário são obrigatórios.");

        if (papel == Perfil.Admin)
            return Result.Failure<MembroEquipe>("O papel do membro na equipe deve ser Gestor ou Analista.");

        return Result.Success(new MembroEquipe(Guid.NewGuid(), tenantId, equipeId, usuarioId, papel));
    }
}
