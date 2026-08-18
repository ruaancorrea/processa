using System.Text.Json;

namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Porta pro transporte HTTP real — o Domain decide o QUE chamar e como avaliar a
/// resposta, mas não conhece HttpClient/IHttpClientFactory (isso é Infrastructure;
/// ver .claude/architecture.md, Domain nunca depende de framework externo).
/// </summary>
public interface IClienteHttpEtapa
{
    Task<RespostaHttpEtapa> EnviarAsync(string url, MetodoHttp metodo, string? corpo, CancellationToken cancellationToken = default);
}

public sealed record RespostaHttpEtapa(bool Sucesso, int? StatusCode, string? ErroMensagem);

/// <summary>
/// Única etapa cujo comportamento real já é totalmente executável no Sprint 4 —
/// não depende de Demanda persistida, só da configuração da própria etapa (PROJ-47).
/// </summary>
public sealed class EtapaAutomatizadaHandler(IClienteHttpEtapa clienteHttp) : IEtapaHandler
{
    public TipoEtapa Tipo => TipoEtapa.Automatizada;

    public async Task<ResultadoExecucaoEtapa> ExecutarAsync(ExecucaoEtapaContexto contexto, CancellationToken cancellationToken = default)
    {
        var configuracao = JsonSerializer.Deserialize<ConfiguracaoEtapaAutomatizada>(contexto.ConfiguracaoJson)
            ?? throw new InvalidOperationException("Configuração da etapa automatizada ausente ou inválida.");

        var resposta = await clienteHttp.EnviarAsync(configuracao.Url, configuracao.Metodo, configuracao.CorpoTemplate, cancellationToken);

        if (!resposta.Sucesso)
            return new ResultadoExecucaoEtapa(DesfechoExecucao.Travada, Motivo: $"Falha ao chamar {configuracao.Url}: {resposta.ErroMensagem}");

        return resposta.StatusCode == configuracao.StatusHttpEsperado
            ? new ResultadoExecucaoEtapa(DesfechoExecucao.Concluida)
            : new ResultadoExecucaoEtapa(
                DesfechoExecucao.Travada,
                Motivo: $"Status HTTP {resposta.StatusCode} diferente do esperado ({configuracao.StatusHttpEsperado}).");
    }
}
