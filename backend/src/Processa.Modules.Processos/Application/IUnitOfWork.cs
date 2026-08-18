namespace Processa.Modules.Processos.Application;

public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
