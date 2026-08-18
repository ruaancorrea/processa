using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Quem pode alterar/concluir esta etapa além do responsável natural da demanda
/// (PROJ-51). Mesma forma de PermissoesInicio (Sprint 3): lista vazia em ambos =
/// sem restrição adicional aqui, não "ninguém pode".
/// </summary>
public sealed class ConfiguracaoAcessoEtapa : ValueObject
{
    public IReadOnlyList<Perfil> PodeAlterar { get; }
    public IReadOnlyList<Guid> UsuarioIdsPodeAlterar { get; }

    private ConfiguracaoAcessoEtapa(IReadOnlyList<Perfil> podeAlterar, IReadOnlyList<Guid> usuarioIdsPodeAlterar)
    {
        PodeAlterar = podeAlterar;
        UsuarioIdsPodeAlterar = usuarioIdsPodeAlterar;
    }

    public static ConfiguracaoAcessoEtapa Vazia => new([], []);

    public static ConfiguracaoAcessoEtapa Criar(IEnumerable<Perfil>? podeAlterar, IEnumerable<Guid>? usuarioIdsPodeAlterar)
    {
        var perfisLista = (podeAlterar ?? []).Distinct().ToList();
        var usuarioIdsLista = (usuarioIdsPodeAlterar ?? []).Where(id => id != Guid.Empty).Distinct().ToList();

        return new ConfiguracaoAcessoEtapa(perfisLista, usuarioIdsLista);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        foreach (var perfil in PodeAlterar.OrderBy(p => p))
            yield return perfil;
        foreach (var usuarioId in UsuarioIdsPodeAlterar.OrderBy(id => id))
            yield return usuarioId;
    }
}
