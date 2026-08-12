using Processa.Modules.Identidade.Application;

namespace Processa.Modules.Identidade.Infrastructure;

/// <summary>bcrypt cost factor 12 — ver docs/05-seguranca/politica-de-seguranca.md#2.</summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int CostFactor = 12;

    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha, CostFactor);

    public bool Verificar(string senha, string hash) => BCrypt.Net.BCrypt.Verify(senha, hash);
}
