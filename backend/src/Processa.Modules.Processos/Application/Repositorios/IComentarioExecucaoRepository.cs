using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IComentarioExecucaoRepository
{
    Task AddAsync(ComentarioExecucao comentario, CancellationToken ct = default);
    Task<List<ComentarioExecucao>> ListarPorExecucaoAsync(Guid execucaoEtapaId, CancellationToken ct = default);
}
