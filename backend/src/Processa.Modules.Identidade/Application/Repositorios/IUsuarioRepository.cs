using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Repositorios;

public interface IUsuarioRepository
{
    /// <summary>
    /// E-mail é único globalmente (não escopado por tenant) — ver .faf/decisions.faf.
    /// Consulta feita fora do Global Query Filter de tenant, pois no momento do login/
    /// cadastro ainda não há tenant resolvido no contexto da requisição.
    /// </summary>
    Task<bool> ExisteEmailAsync(Email email, CancellationToken ct = default);

    Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct = default);

    /// <summary>
    /// Também ignora o Global Query Filter de tenant — usado pelo fluxo de refresh token,
    /// que roda antes de haver um tenant resolvido no contexto (o access token pode já ter
    /// expirado). Nome deliberadamente explícito: um "ObterPorIdAsync" comum poderia ser
    /// chamado por engano por código futuro que espera isolamento normal por tenant, o que
    /// vazaria dados entre tenants silenciosamente.
    /// </summary>
    Task<Usuario?> ObterPorIdIgnorandoTenantAsync(Guid id, CancellationToken ct = default);

    /// <summary>Escopado pelo Global Query Filter de tenant — usar para qualquer
    /// consulta feita dentro de um contexto autenticado (ex.: validar que o usuário
    /// a adicionar numa equipe pertence ao mesmo tenant de quem está adicionando).</summary>
    Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(Usuario usuario, CancellationToken ct = default);
}
