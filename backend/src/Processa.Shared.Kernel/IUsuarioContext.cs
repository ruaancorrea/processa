namespace Processa.Shared.Kernel;

/// <summary>Quem está autenticado na requisição atual — mesmo espírito de ITenantContext.</summary>
public interface IUsuarioContext
{
    Guid UsuarioId { get; }
    Perfil Perfil { get; }
}
