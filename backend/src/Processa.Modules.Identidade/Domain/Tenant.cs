using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

public enum Plano
{
    Trial,
    Starter,
    Pro,
}

public enum StatusTenant
{
    Ativo,
    Suspenso,
}

/// <summary>
/// Um escritório contábil cliente do Processa — a unidade de isolamento de dados.
/// Ver docs/02-arquitetura/decisoes/adr-002-multi-tenancy.md
/// </summary>
public sealed class Tenant : Entity
{
    public string Nome { get; private set; }
    public Cnpj Cnpj { get; private set; }
    public string? DominioPortal { get; private set; }
    public Plano Plano { get; private set; }
    public StatusTenant Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // EF Core materializa via este construtor sem parâmetros + property setters privados.
    private Tenant()
    {
        Nome = string.Empty;
        Cnpj = null!;
    }

    private Tenant(Guid id, string nome, Cnpj cnpj) : base(id)
    {
        Nome = nome;
        Cnpj = cnpj;
        Plano = Plano.Trial;
        Status = StatusTenant.Ativo;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Tenant> Criar(string? nome, string? cnpjBruto)
    {
        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Tenant>("O nome do escritório é obrigatório.");

        var cnpjResult = Cnpj.Criar(cnpjBruto);
        if (cnpjResult.IsFailure)
            return Result.Failure<Tenant>(cnpjResult.Error!);

        var tenant = new Tenant(Guid.NewGuid(), nome.Trim(), cnpjResult.Value);
        tenant.RaiseDomainEvent(new TenantCriadoEvent(tenant.Id, tenant.Nome, tenant.CreatedAt));

        return Result.Success(tenant);
    }

    public void Suspender()
    {
        Status = StatusTenant.Suspenso;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reativar()
    {
        Status = StatusTenant.Ativo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
