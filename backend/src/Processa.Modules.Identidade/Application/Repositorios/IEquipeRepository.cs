using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application.Repositorios;

public interface IEquipeRepository
{
    Task AddAsync(Equipe equipe, CancellationToken ct = default);
    Task<Equipe?> ObterPorIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Equipe>> ListarAsync(CancellationToken ct = default);
}
