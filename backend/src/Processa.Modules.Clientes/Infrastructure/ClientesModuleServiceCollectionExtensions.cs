using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Clientes.Application;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Infrastructure.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure;

public static class ClientesModuleServiceCollectionExtensions
{
    public static IServiceCollection AddClientesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ClientesDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<ClientesDbContext>());

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IGrupoClienteRepository, GrupoClienteRepository>();
        services.AddScoped<IContatoClienteRepository, ContatoClienteRepository>();
        services.AddScoped<IResponsavelClienteRepository, ResponsavelClienteRepository>();
        services.AddScoped<IVerificadorCliente, VerificadorCliente>();
        services.AddScoped<IConsultaCliente, ConsultaCliente>();

        services.AddValidatorsFromAssembly(typeof(ClientesModuleMarker).Assembly);

        return services;
    }
}
