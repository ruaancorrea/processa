using MediatR;

namespace Processa.Shared.Kernel;

/// <summary>
/// Evento de domínio publicado in-process via MediatR. Eventos que precisam
/// atravessar módulos ou disparar trabalho assíncrono são também publicados
/// no barramento de mensageria pela camada de Infrastructure do módulo de origem.
/// Ver docs/02-arquitetura/decisoes/adr-004-mensageria-rabbitmq.md
/// </summary>
public interface IDomainEvent : INotification
{
    Guid TenantId { get; }
    DateTimeOffset OcorridoEm { get; }
}
