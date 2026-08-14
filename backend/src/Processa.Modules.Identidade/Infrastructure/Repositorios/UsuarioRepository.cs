using Microsoft.EntityFrameworkCore;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure.Repositorios;

public sealed class UsuarioRepository(IdentidadeDbContext db) : IUsuarioRepository
{
    // IgnoreQueryFilters(): e-mail é único globalmente, e login/cadastro acontecem
    // antes de existir um tenant resolvido no contexto — exceção legítima e explícita
    // ao Global Query Filter, documentada no ADR-002.
    public Task<bool> ExisteEmailAsync(Email email, CancellationToken ct = default) =>
        db.Usuarios.IgnoreQueryFilters().AnyAsync(u => u.Email.Valor == email.Valor, ct);

    public Task<Usuario?> ObterPorEmailAsync(Email email, CancellationToken ct = default) =>
        db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email.Valor == email.Valor, ct);

    public Task<Usuario?> ObterPorIdIgnorandoTenantAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Usuario?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default) =>
        await db.Usuarios.AddAsync(usuario, ct);
}
