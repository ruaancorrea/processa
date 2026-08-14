using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class VerificadorMembroEquipe(IMembroEquipeRepository membroEquipeRepository) : IVerificadorMembroEquipe
{
    public async Task<bool> EhMembroAsync(Guid equipeId, Guid usuarioId, CancellationToken ct = default) =>
        await membroEquipeRepository.ObterAsync(equipeId, usuarioId, ct) is not null;
}
