namespace Processa.Shared.Kernel;

/// <summary>
/// Contrato de fronteira entre módulos: Clientes precisa saber se um usuário é
/// membro de uma equipe (dono real dessa informação: Identidade), sem que
/// Clientes referencie o assembly de Identidade — nem mesmo a Application layer,
/// pra não criar acoplamento de compilação entre módulos que deveriam poder
/// evoluir/ser extraídos independentemente. A interface mora no Shared.Kernel
/// (visível aos dois lados); a implementação real mora em Identidade e é
/// registrada no DI de lá — inversão de dependência na fronteira do módulo.
/// </summary>
public interface IVerificadorMembroEquipe
{
    Task<bool> EhMembroAsync(Guid equipeId, Guid usuarioId, CancellationToken ct = default);
}
