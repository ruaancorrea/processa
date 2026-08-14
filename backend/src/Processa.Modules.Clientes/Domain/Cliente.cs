using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Domain;

/// <summary>
/// Escritório contábil cliente do tenant (não confundir com Tenant — Tenant é o
/// escritório que assina o Processa, Cliente é o cliente DO escritório). CNPJ
/// único por tenant (não globalmente, ao contrário do e-mail de Usuario — dois
/// tenants diferentes podem legitimamente atender o mesmo CNPJ).
/// </summary>
public sealed class Cliente : Entity
{
    public Guid TenantId { get; private set; }
    public string RazaoSocial { get; private set; }
    public Cnpj Cnpj { get; private set; }
    public string? CodigoExterno { get; private set; }
    public Guid? GrupoClienteId { get; private set; }
    public RegimeTributario RegimeTributario { get; private set; }
    public DateOnly DataEntrada { get; private set; }
    public StatusCliente Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Cliente()
    {
        RazaoSocial = string.Empty;
        Cnpj = null!;
    }

    private Cliente(
        Guid id,
        Guid tenantId,
        string razaoSocial,
        Cnpj cnpj,
        string? codigoExterno,
        Guid? grupoClienteId,
        RegimeTributario regimeTributario,
        DateOnly dataEntrada) : base(id)
    {
        TenantId = tenantId;
        RazaoSocial = razaoSocial;
        Cnpj = cnpj;
        CodigoExterno = codigoExterno;
        GrupoClienteId = grupoClienteId;
        RegimeTributario = regimeTributario;
        DataEntrada = dataEntrada;
        Status = StatusCliente.Ativo;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Cliente> Criar(
        Guid tenantId,
        string? razaoSocial,
        string? cnpjBruto,
        string? codigoExterno,
        Guid? grupoClienteId,
        RegimeTributario regimeTributario,
        DateOnly dataEntrada)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<Cliente>("Tenant inválido.");

        if (string.IsNullOrWhiteSpace(razaoSocial))
            return Result.Failure<Cliente>("A razão social é obrigatória.");

        var cnpjResult = Cnpj.Criar(cnpjBruto);
        if (cnpjResult.IsFailure)
            return Result.Failure<Cliente>(cnpjResult.Error!);

        return Result.Success(new Cliente(
            Guid.NewGuid(), tenantId, razaoSocial.Trim(), cnpjResult.Value,
            codigoExterno?.Trim(), grupoClienteId, regimeTributario, dataEntrada));
    }

    public Result AtualizarDados(string? razaoSocial, string? codigoExterno, Guid? grupoClienteId, RegimeTributario regimeTributario)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            return Result.Failure("A razão social é obrigatória.");

        RazaoSocial = razaoSocial.Trim();
        CodigoExterno = codigoExterno?.Trim();
        GrupoClienteId = grupoClienteId;
        RegimeTributario = regimeTributario;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Success();
    }

    public void Suspender()
    {
        Status = StatusCliente.Suspenso;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Inativar()
    {
        Status = StatusCliente.Inativo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Reativar()
    {
        Status = StatusCliente.Ativo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
