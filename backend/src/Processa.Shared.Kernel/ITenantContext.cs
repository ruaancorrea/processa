namespace Processa.Shared.Kernel;

/// <summary>
/// Resolve o tenant da requisição atual a partir do JWT — usado pelo Global Query Filter
/// do EF Core em cada módulo. Ver docs/02-arquitetura/decisoes/adr-002-multi-tenancy.md
/// </summary>
public interface ITenantContext
{
    Guid TenantId { get; }
}
