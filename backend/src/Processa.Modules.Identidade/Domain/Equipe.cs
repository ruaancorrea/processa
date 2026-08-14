using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// Agrupa usuários pra fins de responsabilidade e dono de tipo de processo
/// (ver docs/01-requisitos/requisitos-funcionais.md#3). Um tipo de processo
/// pertence a exatamente uma equipe (módulo Processos, Sprint 3+).
/// </summary>
public sealed class Equipe : Entity
{
    public Guid TenantId { get; private set; }
    public string Nome { get; private set; }
    public string? Descricao { get; private set; }
    public bool Ativa { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Equipe()
    {
        Nome = string.Empty;
    }

    private Equipe(Guid id, Guid tenantId, string nome, string? descricao) : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Descricao = descricao;
        Ativa = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Equipe> Criar(Guid tenantId, string? nome, string? descricao)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<Equipe>("Tenant inválido.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Equipe>("O nome da equipe é obrigatório.");

        return Result.Success(new Equipe(Guid.NewGuid(), tenantId, nome.Trim(), descricao?.Trim()));
    }

    public Result AtualizarDados(string? nome, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure("O nome da equipe é obrigatório.");

        Nome = nome.Trim();
        Descricao = descricao?.Trim();
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void Desativar()
    {
        Ativa = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reativar()
    {
        Ativa = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
