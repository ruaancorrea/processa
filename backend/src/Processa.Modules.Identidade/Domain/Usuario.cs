using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// Usuário interno do escritório (não confundir com o acesso do cliente no portal —
/// esquema de autenticação separado, ver ADR-005).
/// </summary>
public sealed class Usuario : Entity
{
    private const int MaxTentativasFalhas = 5;
    private static readonly TimeSpan DuracaoBloqueio = TimeSpan.FromMinutes(15);

    public Guid TenantId { get; private set; }
    public string Nome { get; private set; }
    public Email Email { get; private set; }
    public string SenhaHash { get; private set; }
    public Perfil Perfil { get; private set; }
    public int NivelExperiencia { get; private set; }
    public bool Ativo { get; private set; }
    public int TentativasLoginFalhas { get; private set; }
    public DateTimeOffset? BloqueadoAte { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Usuario()
    {
        Nome = string.Empty;
        Email = null!;
        SenhaHash = string.Empty;
    }

    private Usuario(Guid id, Guid tenantId, string nome, Email email, string senhaHash, Perfil perfil) : base(id)
    {
        TenantId = tenantId;
        Nome = nome;
        Email = email;
        SenhaHash = senhaHash;
        Perfil = perfil;
        NivelExperiencia = 1;
        Ativo = true;
        TentativasLoginFalhas = 0;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public static Result<Usuario> Criar(Guid tenantId, string? nome, string? email, string senhaHash, Perfil perfil)
    {
        if (tenantId == Guid.Empty)
            return Result.Failure<Usuario>("Tenant inválido.");

        if (string.IsNullOrWhiteSpace(nome))
            return Result.Failure<Usuario>("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(senhaHash))
            return Result.Failure<Usuario>("A senha é obrigatória.");

        var emailResult = Email.Criar(email);
        if (emailResult.IsFailure)
            return Result.Failure<Usuario>(emailResult.Error!);

        var usuario = new Usuario(Guid.NewGuid(), tenantId, nome.Trim(), emailResult.Value, senhaHash, perfil);
        usuario.RaiseDomainEvent(new UsuarioCriadoEvent(tenantId, usuario.Id, usuario.Email.Valor, usuario.CreatedAt));

        return Result.Success(usuario);
    }

    /// <summary>
    /// true quando o usuário está dentro da janela de bloqueio (ver <see cref="RegistrarTentativaFalha"/>).
    /// </summary>
    public bool EstaBloqueado() => BloqueadoAte is not null && BloqueadoAte > DateTimeOffset.UtcNow;

    /// <summary>
    /// 5 tentativas falhas consecutivas -> bloqueio de 15 minutos. Ver
    /// docs/05-seguranca/politica-de-seguranca.md#1-autenticação-e-autorização
    /// </summary>
    public void RegistrarTentativaFalha()
    {
        // Se a janela de bloqueio anterior já expirou, a contagem reinicia — sem isso,
        // uma única tentativa errada após o desbloqueio automático voltava a bloquear
        // a conta imediatamente (bug real pego em revisão: o contador só zerava em
        // RegistrarLoginSucesso, nunca com a simples passagem do tempo).
        if (BloqueadoAte is not null && BloqueadoAte <= DateTimeOffset.UtcNow)
        {
            TentativasLoginFalhas = 0;
            BloqueadoAte = null;
        }

        TentativasLoginFalhas++;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (TentativasLoginFalhas >= MaxTentativasFalhas)
        {
            BloqueadoAte = DateTimeOffset.UtcNow.Add(DuracaoBloqueio);
            RaiseDomainEvent(new UsuarioBloqueadoEvent(TenantId, Id, BloqueadoAte.Value, UpdatedAt));
        }
    }

    public void RegistrarLoginSucesso()
    {
        TentativasLoginFalhas = 0;
        BloqueadoAte = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Desativar()
    {
        Ativo = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
