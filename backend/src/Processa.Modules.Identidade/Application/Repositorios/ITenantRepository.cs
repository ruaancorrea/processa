using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application.Repositorios;

public interface ITenantRepository
{
    Task<bool> ExisteCnpjAsync(Cnpj cnpj, CancellationToken ct = default);
    Task AddAsync(Tenant tenant, CancellationToken ct = default);
    Task<Tenant?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
}
