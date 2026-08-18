namespace Processa.Shared.Kernel;

/// <summary>
/// Papel do usuário dentro do tenant. Ver docs/01-requisitos/requisitos-funcionais.md#1.
/// Vive em Shared.Kernel pelo mesmo motivo de <see cref="Cnpj"/>/<see cref="Email"/>
/// (Sprint 2) — Identidade (RBAC de API) e Processos (permissões de início de
/// demanda por perfil, Sprint 3) precisam do mesmo vocabulário sem que Processos
/// referencie o Domain de Identidade diretamente.
/// </summary>
public enum Perfil
{
    Admin,
    Gestor,
    Analista,
}
