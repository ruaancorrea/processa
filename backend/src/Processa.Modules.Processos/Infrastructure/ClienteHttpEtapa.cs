using System.Net;
using System.Net.Sockets;
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
            // ResponseHeadersRead: nunca baixamos o corpo da resposta (só o status importa
            // aqui) — evita bufferizar uma resposta arbitrariamente grande de um endpoint
            // configurado por usuário só pra descartar o conteúdo em seguida.
            using var resposta = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return new RespostaHttpEtapa(true, (int)resposta.StatusCode, null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
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

/// <summary>
/// Mitigação de SSRF (achado de revisão de segurança do Sprint 4, .faf/pendencias.faf):
/// a URL de uma Etapa Automatizada é configurada por um Gestor/Admin do tenant, mas nada
/// impede que aponte pra um endpoint interno (ex.: metadata de nuvem 169.254.169.254,
/// serviço interno na rede da aplicação). Validar o HOSTNAME não basta (DNS rebinding: o
/// nome resolve pra um IP público na validação e pra um IP privado na hora de conectar) —
/// por isso o bloqueio acontece no ConnectCallback do SocketsHttpHandler, checando o IP
/// já resolvido, imediatamente antes de abrir a conexão TCP de verdade.
/// </summary>
public static class ClienteHttpEtapaConnectGuard
{
    public static async ValueTask<Stream> ConectarAsync(SocketsHttpConnectionContext contexto, CancellationToken cancellationToken)
    {
        var enderecos = await Dns.GetHostAddressesAsync(contexto.DnsEndPoint.Host, cancellationToken);
        var enderecoPermitido = Array.Find(enderecos, EnderecoEhPermitido)
            ?? throw new InvalidOperationException(
                $"O host '{contexto.DnsEndPoint.Host}' não resolveu para nenhum endereço público permitido (proteção contra SSRF).");

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(enderecoPermitido, contexto.DnsEndPoint.Port, cancellationToken);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Simplificação consciente (ver .faf/pendencias.faf): cobre loopback, link-local
    /// (inclui o metadata de nuvem 169.254.169.254) e os 3 blocos privados IPv4 (RFC 1918),
    /// desembrulhando endereços IPv4-mapeados-em-IPv6 antes de checar. NÃO cobre unique-local
    /// IPv6 (fc00::/7) nem multicast/teredo — risco residual aceito pro MVP, já que o cenário
    /// de ataque mais realista (cloud metadata, rede interna do host) é sempre IPv4.
    /// </summary>
    public static bool EnderecoEhPermitido(IPAddress endereco)
    {
        var enderecoIPv4 = endereco.IsIPv4MappedToIPv6 ? endereco.MapToIPv4() : endereco;

        if (IPAddress.IsLoopback(enderecoIPv4) || enderecoIPv4.IsIPv6LinkLocal || enderecoIPv4.IsIPv6SiteLocal)
            return false;

        if (enderecoIPv4.AddressFamily != AddressFamily.InterNetwork)
            return true;

        var bytes = enderecoIPv4.GetAddressBytes();
        var ehIntervaloPrivadoOuEspecial = bytes[0] switch
        {
            0 => true, // 0.0.0.0/8
            10 => true, // 10.0.0.0/8
            127 => true, // 127.0.0.0/8
            169 when bytes[1] == 254 => true, // 169.254.0.0/16 — inclui o metadata de nuvem
            172 when bytes[1] is >= 16 and <= 31 => true, // 172.16.0.0/12
            192 when bytes[1] == 168 => true, // 192.168.0.0/16
            _ => false,
        };

        return !ehIntervaloPrivadoOuEspecial;
    }
}
