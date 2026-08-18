using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public sealed class VerificadorUsuario(IUsuarioRepository usuarioRepository) : IVerificadorUsuario
{
    public async Task<bool> ExisteAsync(Guid usuarioId, CancellationToken ct = default) =>
        await usuarioRepository.ObterPorIdAsync(usuarioId, ct) is not null;
}
