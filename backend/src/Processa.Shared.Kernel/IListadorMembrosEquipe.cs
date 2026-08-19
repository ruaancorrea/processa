namespace Processa.Shared.Kernel;

/// <summary>
/// "Quem são os membros desta equipe?" — usado pela atribuição Dinâmica (Processos
/// precisa saber os candidatos sem referenciar o Domain de Identidade). Mesmo padrão
/// de IVerificadorMembroEquipe/IVerificadorEquipe, implementado no módulo dono do dado.
/// </summary>
public interface IListadorMembrosEquipe
{
    Task<IReadOnlyList<Guid>> ListarUsuarioIdsAsync(Guid equipeId, CancellationToken ct = default);
}
