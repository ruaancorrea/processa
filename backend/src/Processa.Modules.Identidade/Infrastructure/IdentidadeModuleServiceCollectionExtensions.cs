using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Identidade.Application;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Modules.Identidade.Infrastructure.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Infrastructure;

public static class IdentidadeModuleServiceCollectionExtensions
{
    public static IServiceCollection AddIdentidadeModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpContextTenantContext>();

        services.AddDbContext<IdentidadeDbContext>(opts =>
            opts.UseNpgsql(configuration.GetConnectionString("Postgres")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentidadeDbContext>());

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();

        // ValidateOnStart falha cedo (na subida do host) se Jwt:SecretKey não vier
        // configurado, em vez de deixar o primeiro login/validação de token explodir.
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SecretKey),
                "Configuração 'Jwt:SecretKey' ausente — defina a variável de ambiente Jwt__SecretKey.")
            .ValidateOnStart();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddValidatorsFromAssembly(typeof(IdentidadeModuleMarker).Assembly);

        return services;
    }
}
