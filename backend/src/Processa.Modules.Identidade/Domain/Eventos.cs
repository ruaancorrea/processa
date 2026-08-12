using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Domain;

public sealed record TenantCriadoEvent(Guid TenantId, string Nome, DateTimeOffset OcorridoEm) : IDomainEvent;

public sealed record UsuarioCriadoEvent(Guid TenantId, Guid UsuarioId, string Email, DateTimeOffset OcorridoEm) : IDomainEvent;

public sealed record UsuarioBloqueadoEvent(Guid TenantId, Guid UsuarioId, DateTimeOffset BloqueadoAte, DateTimeOffset OcorridoEm) : IDomainEvent;
