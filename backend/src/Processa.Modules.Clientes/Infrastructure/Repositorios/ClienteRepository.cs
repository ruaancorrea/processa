using Microsoft.EntityFrameworkCore;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Infrastructure.Repositorios;

public sealed class ClienteRepository(ClientesDbContext db) : IClienteRepository
{
    public async Task AddAsync(Cliente cliente, CancellationToken ct = default) =>
        await db.Clientes.AddAsync(cliente, ct);

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    // Compara o VO inteiro (não c.Cnpj.Numero) — Cnpj é mapeado via HasConversion,
    // então só a comparação da propriedade convertida como um todo traduz pra SQL;
    // acessar .Numero depois da conversão não seria traduzível.
    public Task<bool> ExisteCnpjAsync(Cnpj cnpj, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.Cnpj == cnpj, ct);

    public Task<List<Cliente>> ListarAsync(CancellationToken ct = default) =>
        db.Clientes.OrderBy(c => c.RazaoSocial).ToListAsync(ct);
}
