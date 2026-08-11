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

// CORS restrito a origens explicitamente autorizadas (nunca AllowAnyOrigin) — ver
// docs/05-seguranca/politica-de-seguranca.md#2-proteção-de-dados. Origens configuradas
// em appsettings.{Environment}.json / Cors:AllowedOrigins (dev: o painel React local).
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// Documentação OpenAPI/Swagger interativa — ver docs/04-api/convencoes-api.md
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
    opts.SwaggerDoc("v1", new() { Title = "Processa API", Version = "v1" }));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthorization();

app.MapControllers();
app.MapProcessosModule();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
    .WithTags("Health");

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração encontrar o entry point.
public partial class Program;
