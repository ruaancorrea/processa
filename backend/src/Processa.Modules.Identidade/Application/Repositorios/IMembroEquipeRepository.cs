using Processa.Modules.Identidade.Domain;

namespace Processa.Modules.Identidade.Application.Repositorios;

public interface IMembroEquipeRepository
{
    Task AddAsync(MembroEquipe membro, CancellationToken ct = default);
    Task<MembroEquipe?> ObterAsync(Guid equipeId, Guid usuarioId, CancellationToken ct = default);
    Task<List<MembroEquipe>> ListarPorEquipeAsync(Guid equipeId, CancellationToken ct = default);
    void Remover(MembroEquipe membro);
}
