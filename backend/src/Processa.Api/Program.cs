using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Processa.Api;
using Processa.Modules.Clientes.Infrastructure;
using Processa.Modules.Clientes.Presentation;
using Processa.Modules.Identidade.Infrastructure;
using Processa.Modules.Identidade.Presentation;
using Processa.Modules.Processos.Infrastructure;
using Processa.Modules.Processos.Presentation;
using Processa.Shared.Kernel;

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
// ValidationBehavior roda todo IValidator<T> registrado antes do handler (FluentValidation).
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<Processa.Modules.Processos.ProcessosModuleMarker>();
    cfg.RegisterServicesFromAssemblyContaining<Processa.Modules.Identidade.IdentidadeModuleMarker>();
    cfg.RegisterServicesFromAssemblyContaining<Processa.Modules.Clientes.ClientesModuleMarker>();
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddIdentidadeModule(builder.Configuration);
builder.Services.AddClientesModule(builder.Configuration);
builder.Services.AddProcessosModule(builder.Configuration);

// Middleware global de exceção (RFC 9457 Problem Details) — ver GlobalExceptionHandler.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT: emitido por Modules.Identidade (JwtTokenGenerator), validado aqui na composition
// root. As duas pontas leem a MESMA IOptions<JwtOptions> (bindada em AddIdentidadeModule)
// em vez de cada uma ler builder.Configuration por conta própria — evitava divergência
// sutil entre o valor visto na emissão e o visto na validação (bug real pego em teste de
// integração: WebApplicationFactory sobrescreve configuração via ConfigureAppConfiguration,
// mas uma leitura direta e antecipada de builder.Configuration em Program.cs não enxergava
// esse override a tempo, enquanto o IOptions<T>, resolvido só quando o handler realmente
// valida um token, sempre enxerga a configuração final).
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearerOpts, jwtOpts) =>
    {
        var jwt = jwtOpts.Value;
        // Sem isso, o JwtSecurityTokenHandler renomeia claims curtas conhecidas (ex.: "sub"
        // -> ClaimTypes.NameIdentifier) na validação, e FindFirst(JwtRegisteredClaimNames.Sub)
        // no endpoint /me nunca encontra nada (bug real pego em verificação manual — os testes
        // automatizados não liam usuarioId da resposta).
        bearerOpts.MapInboundClaims = false;
        bearerOpts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });

// RBAC por policy — nunca checagem manual de string de perfil nos endpoints/controllers.
// Ver docs/02-arquitetura/decisoes/adr-005-autenticacao-multi-perfil.md
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", p => p.RequireRole("Admin"))
    .AddPolicy("GestorOuAdmin", p => p.RequireRole("Gestor", "Admin"))
    .AddPolicy("QualquerPerfil", p => p.RequireRole("Admin", "Gestor", "Analista"));

// CORS restrito a origens explicitamente autorizadas (nunca AllowAnyOrigin) — ver
// docs/05-seguranca/politica-de-seguranca.md#2-proteção-de-dados. Origens configuradas
// em appsettings.{Environment}.json / Cors:AllowedOrigins (dev: o painel React local).
// AllowCredentials necessário para o cookie httpOnly do refresh token atravessar CORS.
const string FrontendCorsPolicy = "FrontendCorsPolicy";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Documentação OpenAPI/Swagger interativa — ver docs/04-api/convencoes-api.md
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
    opts.SwaggerDoc("v1", new() { Title = "Processa API", Version = "v1" }));

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapProcessosModule();
app.MapConfiguracaoProcessosModule();
app.MapIdentidadeModule();
app.MapEquipesModule();
app.MapClientesModule();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }))
    .WithTags("Health");

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração encontrar o entry point.
public partial class Program;
