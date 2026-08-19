using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Application.Repositorios;

public interface IDesdobramentoAguardadoRepository
{
    Task AddAsync(DesdobramentoAguardado desdobramento, CancellationToken ct = default);
    Task<List<DesdobramentoAguardado>> ListarPorExecucaoUniaoAsync(Guid execucaoEtapaUniaoId, CancellationToken ct = default);
    Task<DesdobramentoAguardado?> ObterPorExecucaoCondicionalAsync(Guid execucaoEtapaCondicionalId, CancellationToken ct = default);
}
