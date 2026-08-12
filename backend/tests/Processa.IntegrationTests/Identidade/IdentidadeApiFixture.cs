using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Processa.Modules.Identidade.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace Processa.IntegrationTests.Identidade;

/// <summary>
/// Postgres real via Testcontainers (não H2/sqlite in-memory — queremos pegar diferenças
/// reais de dialeto/índice/constraint do Postgres). Compartilhado entre todos os testes
/// da coleção "Identidade" (ver <see cref="IdentidadeTestCollection"/>) — um container só,
/// migrations aplicadas uma vez.
/// </summary>
public sealed class IdentidadeApiFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("processa_testes")
        .WithUsername("processa")
        .WithPassword("processa")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "processa-testes-integracao",
                ["Jwt:Audience"] = "processa-testes-integracao-app",
                ["Jwt:SecretKey"] = "chave-de-teste-de-integracao-nunca-usar-em-producao-32c",
                ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
            });
        });
    }

    /// <summary>
    /// O cookie de refresh é Secure=true (nunca deve trafegar em texto claro em produção).
    /// O CookieContainer do HttpClient respeita essa flag e recusa a reenviar o cookie numa
    /// requisição http://; como o BaseAddress padrão do WebApplicationFactory é http://localhost,
    /// isso fazia o cookie "sumir" nos testes mesmo tendo sido setado corretamente. ConfigureClient
    /// não resolve — CreateDefaultClient sobrescreve BaseAddress depois dele rodar. O TestServer é
    /// in-memory (não faz handshake TLS de verdade), então forçar https:// aqui só destrava o
    /// CookieContainer, sem exigir certificado nenhum.
    /// </summary>
    public new HttpClient CreateClient(WebApplicationFactoryClientOptions? options = null)
    {
        options ??= new WebApplicationFactoryClientOptions();
        options.BaseAddress = new Uri("https://localhost");
        return base.CreateClient(options);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentidadeDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>Limpa as tabelas entre testes sem recriar o container (rápido).</summary>
    public async Task LimparDadosAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentidadeDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE identidade.refresh_tokens, identidade.usuarios, identidade.tenants CASCADE;");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

[CollectionDefinition("Identidade")]
public sealed class IdentidadeTestCollection : ICollectionFixture<IdentidadeApiFixture>;
