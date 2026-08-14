namespace Processa.Modules.Clientes.Application;

public interface IUnitOfWork
{
    Task SalvarAsync(CancellationToken ct = default);
}
