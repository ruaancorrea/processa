namespace Processa.Modules.Identidade.Infrastructure;

/// <summary>
/// Lido de configuração (appsettings + variável de ambiente Jwt__SecretKey) — nunca
/// hardcoded. Ver .env.example e docs/05-seguranca/politica-de-seguranca.md#6.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public required string Issuer { get; init; }
    public required string Audience { get; init; }
    public required string SecretKey { get; init; }
    public int AccessTokenMinutos { get; init; } = 15;
}
