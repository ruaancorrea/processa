using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class VerificadorEquipe(IEquipeRepository equipeRepository) : IVerificadorEquipe
{
    public async Task<bool> ExisteAsync(Guid equipeId, CancellationToken ct = default) =>
        await equipeRepository.ObterPorIdAsync(equipeId, ct) is not null;
}
