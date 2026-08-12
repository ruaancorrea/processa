using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Tenants;

/// <summary>
/// Onboarding: cria o tenant (escritório) e seu primeiro usuário, já como admin.
/// Endpoint público — não exige autenticação prévia (é como um novo cliente nasce).
/// </summary>
public sealed record CriarTenantCommand(
    string NomeEscritorio,
    string Cnpj,
    string NomeAdmin,
    string EmailAdmin,
    string Senha) : IRequest<Result<CriarTenantResultado>>;

public sealed record CriarTenantResultado(Guid TenantId, Guid UsuarioAdminId);

public sealed class CriarTenantCommandHandler(
    ITenantRepository tenantRepository,
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarTenantCommand, Result<CriarTenantResultado>>
{
    public async Task<Result<CriarTenantResultado>> Handle(CriarTenantCommand request, CancellationToken cancellationToken)
    {
        var tenantResult = Tenant.Criar(request.NomeEscritorio, request.Cnpj);
        if (tenantResult.IsFailure)
            return Result.Failure<CriarTenantResultado>(tenantResult.Error!);

        var tenant = tenantResult.Value;

        if (await tenantRepository.ExisteCnpjAsync(tenant.Cnpj, cancellationToken))
            return Result.Failure<CriarTenantResultado>("Já existe um escritório cadastrado com este CNPJ.");

        var emailResult = Email.Criar(request.EmailAdmin);
        if (emailResult.IsFailure)
            return Result.Failure<CriarTenantResultado>(emailResult.Error!);

        if (await usuarioRepository.ExisteEmailAsync(emailResult.Value, cancellationToken))
            return Result.Failure<CriarTenantResultado>("Já existe um usuário cadastrado com este e-mail.");

        var senhaHash = passwordHasher.Hash(request.Senha);
        var usuarioResult = Usuario.Criar(tenant.Id, request.NomeAdmin, request.EmailAdmin, senhaHash, Perfil.Admin);
        if (usuarioResult.IsFailure)
            return Result.Failure<CriarTenantResultado>(usuarioResult.Error!);

        await tenantRepository.AddAsync(tenant, cancellationToken);
        await usuarioRepository.AddAsync(usuarioResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(new CriarTenantResultado(tenant.Id, usuarioResult.Value.Id));
    }
}
