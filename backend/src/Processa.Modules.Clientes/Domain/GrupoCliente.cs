using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Domain;

/// <summary>Agrupamento lógico de clientes para histórico compartilhado e notificações coletivas.</summary>
public sealed class GrupoCliente : Entity
{
    public Guid TenantId { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private GrupoCliente()
    {
        Nome = string.Empty;
    }

    private GrupoCliente(Guid id, Guid tenantId, string nome, string? descricao) : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Descricao = descricao;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<GrupoCliente> Criar(Guid tenantId, string? nome, string? descricao)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<GrupoCliente>("Tenant inválido.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<GrupoCliente>("O nome do grupo é obrigatório.");

        return Result.Success(new GrupoCliente(Guid.NewGuid(), tenantId, nome.Trim(), descricao?.Trim()));
    }
}
