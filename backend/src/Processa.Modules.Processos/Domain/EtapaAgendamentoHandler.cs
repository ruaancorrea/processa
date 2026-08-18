using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>Aguarda um número de dias corridos desde o início da execução, depois conclui sozinha (PROJ-48).</summary>
public sealed class EtapaAgendamentoHandler : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Agendamento;

    public Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        var configuracao = JsonSerializer.Deserialize<ConfiguracaoEtapaAgendamento>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa de agendamento ausente ou inválida.");

        var iniciadoEm = contexto.IniciadoEm ?? DateTimeOffset.UtcNow;
        var dataAlvo = iniciadoEm.AddDays(configuracao.DiasOffset);

        var resultado = DateTimeOffset.UtcNow >= dataAlvo
            ? new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida)
            : new ResultadoExecucaoEtapa(DesfechoExecucao.Aguardando, Motivo: $"Aguardando até {dataAlvo:O}.");

        return Task.FromResult(resultado);
    }
}
