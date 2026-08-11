using System.Text.Json.Serialization;
using Processa.Modules.Processos.Presentation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Enums sempre serializados como string (não índice numérico) — API pública
// autoexplicativa, sem exigir consulta à documentação para decodificar um valor.
// Ver docs/04-api/convencoes-api.md
builder.Services.AddControllers()
    .AddJsonOptions(opts => opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.ConfigureHttpJsonOptions(opts => opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// MediatR varre os assemblies de todos os módulos (Application layer de cada um)
// a partir de um marcador por módulo — nenhum módulo referencia outro diretamente.
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Processa.Modules.Processos.ProcessosModuleMarker>());

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapProcessosModule();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
    .WithTags("Health");

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração encontrar o entry point.
public partial class Program;
