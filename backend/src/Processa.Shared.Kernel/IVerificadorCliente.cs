namespace Processa.Shared.Kernel;

/// <summary>"Esse cliente existe (e está ativo)?" — Processos precisa validar sem referenciar o Domain de Clientes.</summary>
public interface IVerificadorCliente
{
    Task<bool> ExisteAtivoAsync(Guid clienteId, CancellationToken ct = default);
}
