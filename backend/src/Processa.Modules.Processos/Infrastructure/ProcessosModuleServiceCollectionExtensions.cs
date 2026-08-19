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
        services.AddScoped<IDemandaRepository, DemandaRepository>();
        services.AddScoped<IExecucaoEtapaRepository, ExecucaoEtapaRepository>();
        services.AddScoped<IDesdobramentoAguardadoRepository, DesdobramentoAguardadoRepository>();
        services.AddScoped<IHistoricoExecucaoEtapaRepository, HistoricoExecucaoEtapaRepository>();
        services.AddScoped<IComentarioExecucaoRepository, ComentarioExecucaoRepository>();
        services.AddScoped<IAnexoExecucaoRepository, AnexoExecucaoRepository>();

        services.AddValidatorsFromAssembly(typeof(ProcessosModuleMarker).Assembly);

        // Motor de processos (ADR-003): os 8 handlers + a factory que os resolve por
        // TipoEtapa. Desde o Sprint 5 todos os adapters são reais — os 4 que eram
        // stub "NaoImplementado" no Sprint 4 (ResolvedorValorCampo, NotificadorEtapa,
        // CriadorSubprocesso, VerificadorDesdobramentos) foram substituídos.
        // Timeout curto + ConnectCallback bloqueando IP privado/link-local: mitigação de
        // SSRF (.faf/pendencias.faf) — a URL é configurada por um Gestor/Admin do tenant,
        // então nada garante que aponte só pra endpoints públicos de verdade.
        services.AddHttpClient(nameof(ClienteHttpEtapa), client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ConnectCallback = ClienteHttpEtapaConnectGuard.ConectarAsync,
            });
        services.AddScoped<IClienteHttpEtapa, ClienteHttpEtapa>();
        services.AddScoped<IResolvedorValorCampo, ResolvedorValorCampo>();
        services.AddScoped<INotificadorEtapa, NotificadorEtapa>();
        services.AddScoped<ICriadorSubprocesso, CriadorSubprocesso>();
        services.AddScoped<IVerificadorDesdobramentos, VerificadorDesdobramentos>();

        services.AddScoped<Application.Demandas.OrquestradorExecucao>();
        services.Configure<MinioOptions>(configuration.GetSection(MinioOptions.SectionName));
        // Singleton (não Scoped): AmazonS3Client é thread-safe e caro de construir —
        // mesma recomendação da AWS. Também é o que faz o cache de bucket confirmado
        // em ArmazenamentoArquivoS3 funcionar (ver comentário na classe).
        services.AddSingleton<IArmazenamentoArquivo, ArmazenamentoArquivoS3>();

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
