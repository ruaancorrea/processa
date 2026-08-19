using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class ListadorMembrosEquipe(IMembroEquipeRepository membroEquipeRepository) : IListadorMembrosEquipe
{
    public async Task<IReadOnlyList<Guid>> ListarUsuarioIdsAsync(Guid equipeId, CancellationToken ct = default)
    {
        var membros = await membroEquipeRepository.ListarPorEquipeAsync(equipeId, ct);
        return membros.Select(m => m.UsuarioId).ToList();
    }
}
