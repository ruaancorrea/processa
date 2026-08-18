using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Quais perfis e/ou usuários específicos podem abrir uma demanda de um tipo de
/// processo. Lista vazia em ambos = ninguém restrito por perfil/usuário aqui —
/// a checagem de "quem pode iniciar" no Sprint 5 (execução) trata lista vazia
/// como "sem restrição adicional", não como "ninguém pode".
/// </summary>
public sealed class PermissoesInicio : ValueObject
{
    public IReadOnlyList<Perfil> Perfis { get; }
    public IReadOnlyList<Guid> UsuarioIds { get; }

    private PermissoesInicio(IReadOnlyList<Perfil> perfis, IReadOnlyList<Guid> usuarioIds)
    {
        Perfis = perfis;
        UsuarioIds = usuarioIds;
    }

    public static PermissoesInicio Vazia => new([], []);

    public static PermissoesInicio Criar(IEnumerable<Perfil>? perfis, IEnumerable<Guid>? usuarioIds)
    {
        var perfisLista = (perfis ?? []).Distinct().ToList();
        var usuarioIdsLista = (usuarioIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();

        return new PermissoesInicio(perfisLista, usuarioIdsLista);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var perfil in Perfis.OrderBy(p => p))
            yield return perfil;
        foreach (var usuarioId in UsuarioIds.OrderBy(id => id))
            yield return usuarioId;
    }
}
