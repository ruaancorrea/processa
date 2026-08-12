namespace Processa.Modules.Identidade.Application;

/// <summary>Commit explícito das alterações rastreadas pelo DbContext do módulo.</summary>
public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
