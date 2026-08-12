namespace Processa.Modules.Identidade.Domain;

/// <summary>
/// Papel do usuário dentro do tenant. Ver docs/01-requisitos/requisitos-funcionais.md#1.
/// </summary>
public enum Perfil
{
    Admin,
    Gestor,
    Analista,
}
