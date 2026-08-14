using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Domain;

/// <summary>Pessoa de contato do cliente — selecionável individualmente ou em bloco nas notificações.</summary>
public sealed class ContatoCliente : Entity
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Nome { get; private set; }
    public Email? Email { get; private set; }
    public string? Telefone { get; private set; }
    public string? Celular { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private ContatoCliente()
    {
        Nome = string.Empty;
    }

    private ContatoCliente(Guid id, Guid tenantId, Guid clienteId, string nome, Email? email, string? telefone, string? celular) : base(id)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        Nome = nome;
        Email = email;
        Telefone = telefone;
        Celular = celular;
        Ativo = true;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<ContatoCliente> Criar(
        Guid tenantId, Guid clienteId, string? nome, string? emailBruto, string? telefone, string? celular)
    {
        if (tenantId == Guid.Empty || clienteId == Guid.Empty)
            return Result.Failure<ContatoCliente>("Tenant e cliente são obrigatórios.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<ContatoCliente>("O nome do contato é obrigatório.");

        Email? email = null;
        if (!string.IsNullOrWhiteSpace(emailBruto))
        {
            var emailResult = Email.Criar(emailBruto);
            if (emailResult.IsFailure)
                return Result.Failure<ContatoCliente>(emailResult.Error!);
            email = emailResult.Value;
        }

        if (email is null && string.IsNullOrWhiteSpace(telefone) && string.IsNullOrWhiteSpace(celular))
            return Result.Failure<ContatoCliente>("Informe ao menos um meio de contato (e-mail, telefone ou celular).");

        return Result.Success(new ContatoCliente(Guid.NewGuid(), tenantId, clienteId, nome.Trim(), email, telefone?.Trim(), celular?.Trim()));
    }

    public void Desativar()
    {
        Ativo = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reativar()
    {
        Ativo = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
