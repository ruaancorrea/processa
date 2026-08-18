using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Resolve o valor de um campo personalizado preenchido na demanda em execução —
/// a implementação real (lendo dados_execucao de uma ExecucaoEtapa/Demanda de
/// verdade) é Infrastructure do Sprint 5; o handler só depende da decisão.
/// </summary>
public interface IResolvedorValorCampo
{
    Task<string?> ObterValorAsync(Guid tenantId, Guid demandaId, Guid campoPersonalizadoId, CancellationToken cancellationToken = default);
}

/// <summary>Avalia os ramos em ordem; o primeiro que casar decide o desvio de fluxo (PROJ-46).</summary>
public sealed class EtapaCondicionalHandler(IResolvedorValorCampo resolvedorValorCampo) : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Condicional;

    public async Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        var configuracao = JsonSerializer.Deserialize<ConfiguracaoEtapaCondicional>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa condicional ausente ou inválida.");

        foreach (var ramo in configuracao.Ramos)
        {
            var valorReal = await resolvedorValorCampo.ObterValorAsync(contexto.TenantId, contexto.DemandaId, ramo.CampoPersonalizadoId, cancellationToken);
            if (Avalia(ramo.Operador, valorReal, ramo.Valor))
                return new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, ramo.EtapaDestinoId);
        }

        return new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida, configuracao.EtapaPadraoId, "Nenhum ramo casou; seguiu para a etapa padrão.");
    }

    public static bool Avalia(OperadorCondicional operador, string? valorReal, string valorEsperado) => operador switch
    {
        OperadorCondicional.Igual => string.Equals(valorReal, valorEsperado, StringComparison.OrdinalIgnoreCase),
        OperadorCondicional.Diferente => !string.Equals(valorReal, valorEsperado, StringComparison.OrdinalIgnoreCase),
        OperadorCondicional.Contem => valorReal is not null && valorReal.Contains(valorEsperado, StringComparison.OrdinalIgnoreCase),
        OperadorCondicional.MaiorQue => ComparaNumerico(valorReal, valorEsperado) is > 0,
        OperadorCondicional.MenorQue => ComparaNumerico(valorReal, valorEsperado) is < 0,
        _ => false,
    };

    private static int? ComparaNumerico(string? valorReal, string valorEsperado) =>
        decimal.TryParse(valorReal, out var real) && decimal.TryParse(valorEsperado, out var esperado)
            ? real.CompareTo(esperado)
            : null;
}
