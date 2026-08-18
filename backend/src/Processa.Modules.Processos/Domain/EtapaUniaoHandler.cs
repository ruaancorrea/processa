using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Porta pra checar o estado real dos desdobramentos aguardados (tabela
/// desdobramentos_aguardados, ver docs/03-modelagem/modelo-de-dados.md) — a
/// persistência real é Sprint 5; o handler só decide avançar ou aguardar.
/// </summary>
public interface IVerificadorDesdobramentos
{
    Task<bool> TodosConcluidosAsync(Guid tenantId, Guid execucaoEtapaUniaoId, CancellationToken cancellationToken = default);
}

/// <summary>Fork/join: só avança quando todos os ramos que convergem nela concluíram (PROJ-50).</summary>
public sealed class EtapaUniaoHandler(IVerificadorDesdobramentos verificadorDesdobramentos) : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Uniao;

    public async Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        // Só pra validar a forma da configuração — a decisão de avançar depende do
        // estado real dos desdobramentos, não da lista de ids em si.
        _ = JsonSerializer.Deserialize<ConfiguracaoEtapaUniao>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa de união ausente ou inválida.");

        var todosConcluidos = await verificadorDesdobramentos.TodosConcluidosAsync(contexto.TenantId, contexto.ExecucaoEtapaId, cancellationToken);

        return todosConcluidos
            ? new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida)
            : new ResultadoExecucaoEtapa(DesfechoExecucao.Aguardando, Motivo: "Aguardando os demais ramos concluírem.");
    }
}
