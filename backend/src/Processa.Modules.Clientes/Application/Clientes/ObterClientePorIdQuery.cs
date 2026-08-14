using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed record ContatoResumo(Guid Id, string Nome, string? Email, string? Telefone, string? Celular, bool Ativo);

public sealed record ResponsavelResumo(Guid Id, Guid EquipeId, Guid UsuarioId);

public sealed record ClienteDetalhe(
    Guid Id,
    string RazaoSocial,
    string Cnpj,
    string? CodigoExterno,
    Guid? GrupoClienteId,
    RegimeTributario RegimeTributario,
    DateOnly DataEntrada,
    StatusCliente Status,
    List<ContatoResumo> Contatos,
    List<ResponsavelResumo> Responsaveis);

public sealed record ObterClientePorIdQuery(Guid ClienteId) : IRequest<Result<ClienteDetalhe>>;

public sealed class ObterClientePorIdQueryHandler(
    IClienteRepository clienteRepository,
    IContatoClienteRepository contatoClienteRepository,
    IResponsavelClienteRepository responsavelClienteRepository) : IRequestHandler<ObterClientePorIdQuery, Result<ClienteDetalhe>>
{
    public async Task<Result<ClienteDetalhe>> Handle(ObterClientePorIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure<ClienteDetalhe>("Cliente não encontrado.");

        var contatos = await contatoClienteRepository.ListarPorClienteAsync(request.ClienteId, cancellationToken);
        var responsaveis = await responsavelClienteRepository.ListarAtivosPorClienteAsync(request.ClienteId, cancellationToken);

        return Result.Success(new ClienteDetalhe(
            cliente.Id, cliente.RazaoSocial, cliente.Cnpj.Numero, cliente.CodigoExterno, cliente.GrupoClienteId,
            cliente.RegimeTributario, cliente.DataEntrada, cliente.Status,
            contatos.Select(c => new ContatoResumo(c.Id, c.Nome, c.Email?.Valor, c.Telefone, c.Celular, c.Ativo)).ToList(),
            responsaveis.Select(r => new ResponsavelResumo(r.Id, r.EquipeId, r.UsuarioId)).ToList()));
    }
}
