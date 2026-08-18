using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>Adapter real (não stub) de IClienteHttpEtapa — a única dependência de handler que já roda de verdade no Sprint 4.</summary>
public sealed class ClienteHttpEtapa(IHttpClientFactory httpClientFactory) : IClienteHttpEtapa
{
    public async Task<RespostaHttpEtapa> EnviarAsync(string url, MetodoHttp metodo, string? corpo, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(nameof(ClienteHttpEtapa));
        using var request = new HttpRequestMessage(ParaHttpMethod(metodo), url);
        if (corpo is not null)
            request.Content = new StringContent(corpo, System.Text.Encoding.UTF8, "application/json");

        try
        {
            using var resposta = await client.SendAsync(request, cancellationToken);
            return new RespostaHttpEtapa(true, (int)resposta.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return new RespostaHttpEtapa(false, null, ex.Message);
        }
    }

    private static HttpMethod ParaHttpMethod(MetodoHttp metodo) => metodo switch
    {
        MetodoHttp.Get => HttpMethod.Get,
        MetodoHttp.Post => HttpMethod.Post,
        MetodoHttp.Put => HttpMethod.Put,
        _ => throw new ArgumentOutOfRangeException(nameof(metodo), metodo, null),
    };
}
