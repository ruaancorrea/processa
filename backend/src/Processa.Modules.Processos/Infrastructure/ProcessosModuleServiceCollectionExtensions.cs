using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Modules.Processos.Infrastructure.Repositorios;

namespace Processa.Modules.Processos.Infrastructure;

public static class ProcessosModuleServiceCollectionExtensions
{
    public static IServiceCollection AddProcessosModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProcessosDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ProcessosDbContext>());

        services.AddScoped<ITipoProcessoRepository, TipoProcessoRepository>();
        services.AddScoped<ICampoPersonalizadoRepository, CampoPersonalizadoRepository>();
        services.AddScoped<IFluxoRepository, FluxoRepository>();
        services.AddScoped<IEtapaRepository, EtapaRepository>();

        services.AddValidatorsFromAssembly(typeof(ProcessosModuleMarker).Assembly);

        // Motor de processos (ADR-003): os 8 handlers + a factory que os resolve por
        // TipoEtapa. IClienteHttpEtapa já é real (Etapa Automatizada roda de verdade
        // desde o Sprint 4); os outros 4 adapters são stubs que falham alto até o
        // Sprint 5 implementar a persistência real de Demanda/ExecucaoEtapa.
        services.AddHttpClient(nameof(ClienteHttpEtapa));
        services.AddScoped<IClienteHttpEtapa, ClienteHttpEtapa>();
        services.AddScoped<IResolvedorValorCampo, ResolvedorValorCampoNaoImplementado>();
        services.AddScoped<INotificadorEtapa, NotificadorEtapaNaoImplementado>();
        services.AddScoped<ICriadorSubprocesso, CriadorSubprocessoNaoImplementado>();
        services.AddScoped<IVerificadorDesdobramentos, VerificadorDesdobramentosNaoImplementado>();

        services.AddScoped<IEtapaHandler, EtapaComumHandler>();
        services.AddScoped<IEtapaHandler, EtapaConclusaoHandler>();
        services.AddScoped<IEtapaHandler, EtapaCondicionalHandler>();
        services.AddScoped<IEtapaHandler, EtapaAutomatizadaHandler>();
        services.AddScoped<IEtapaHandler, EtapaNotificacaoHandler>();
        services.AddScoped<IEtapaHandler, EtapaAgendamentoHandler>();
        services.AddScoped<IEtapaHandler, EtapaSubprocessoHandler>();
        services.AddScoped<IEtapaHandler, EtapaUniaoHandler>();
        services.AddScoped<EtapaHandlerFactory>();

        return services;
    }
}
