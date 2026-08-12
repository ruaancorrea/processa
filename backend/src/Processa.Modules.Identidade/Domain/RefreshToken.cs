using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// Refresh token opaco (não-JWT), 7 dias, armazenado como hash — nunca em texto puro.
/// Reuse-detection (ADR-005) fica documentado como pendência — ver .faf/pendencias.faf;
/// esta primeira versão suporta revogação explícita (logout) e expiração, não detecção
/// automática de reuso de token já consumido.
/// </summary>
public sealed class RefreshToken : Entity
{
    private static readonly TimeSpan Validade = TimeSpan.FromDays(7);

    public Guid UsuarioId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiraEm { get; private set; }
    public DateTimeOffset? RevogadoEm { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    private RefreshToken(Guid id, Guid usuarioId, string tokenHash) : base(id)
    {
        UsuarioId = usuarioId;
        TokenHash = tokenHash;
        CreatedAt = DateTimeOffset.UtcNow;
        ExpiraEm = CreatedAt.Add(Validade);
    }

    public static RefreshToken Criar(Guid usuarioId, string tokenHash) =>
        new(Guid.NewGuid(), usuarioId, tokenHash);

    public bool EhValido() => RevogadoEm is null && ExpiraEm > DateTimeOffset.UtcNow;

    public void Revogar() => RevogadoEm = DateTimeOffset.UtcNow;
}
