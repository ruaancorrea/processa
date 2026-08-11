using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Processa.Modules.Processos.Application.Etapas;

namespace Processa.Modules.Processos.Presentation;

/// <summary>
/// Minimal API do módulo Processos. Registrado pela composition root (Processa.Api)
/// via <see cref="MapProcessosModule"/> — o módulo não conhece o host, apenas expõe rotas.
/// </summary>
public static class TiposEtapaEndpoints
{
    public static IEndpointRouteBuilder MapProcessosModule(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/tipos-etapa", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new ListarTiposEtapaQuery(), ct)))
            .WithName("ListarTiposEtapa")
            .WithTags("Processos");

        return app;
    }
}
