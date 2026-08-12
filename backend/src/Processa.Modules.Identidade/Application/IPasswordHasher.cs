namespace Processa.Modules.Identidade.Application;

/// <summary>bcrypt, cost factor >= 12 — ver docs/05-seguranca/politica-de-seguranca.md.</summary>
public interface IPasswordHasher
{
    string Hash(string senha);
    bool Verificar(string senha, string hash);
}
