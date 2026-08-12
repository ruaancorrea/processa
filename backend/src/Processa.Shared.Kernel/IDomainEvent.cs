namespace Processa.Shared.Kernel;

/// <summary>
/// Marcador puro, sem dependência de framework — Domain não pode referenciar MediatR
/// (ver Processa.ArchitectureTests.Domain_NaoDependeDeFrameworksExternos). O despacho
/// (in-process via MediatR e/ou barramento de mensageria) é responsabilidade da camada
/// de Infrastructure do módulo de origem, que adapta IDomainEvent para INotification no
/// ponto de publicação. Ver docs/02-arquitetura/decisoes/adr-004-mensageria-rabbitmq.md
/// </summary>
public interface IDomainEvent
{
    Guid TenantId { get; }
    DateTimeOffset OcorridoEm { get; }
}
