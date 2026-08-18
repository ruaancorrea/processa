using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Processos.Application;
using Processa.Modules.Processos.Application.Repositorios;
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

        services.AddValidatorsFromAssembly(typeof(ProcessosModuleMarker).Assembly);

        return services;
    }
}
