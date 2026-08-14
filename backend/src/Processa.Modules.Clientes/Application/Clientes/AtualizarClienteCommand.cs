using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed record AtualizarClienteCommand(
    Guid ClienteId,
    string RazaoSocial,
    string? CodigoExterno,
    Guid? GrupoClienteId,
    RegimeTributario RegimeTributario) : IRequest<Result>;

public sealed class AtualizarClienteCommandHandler(
    IClienteRepository clienteRepository, IGrupoClienteRepository grupoClienteRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<AtualizarClienteCommand, Result>
{
    public async Task<Result> Handle(AtualizarClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure("Cliente não encontrado.");

        if (request.GrupoClienteId is { } grupoId)
        {
            var grupo = await grupoClienteRepository.ObterPorIdAsync(grupoId, cancellationToken);
            if (grupo is null)
                return Result.Failure("Grupo de clientes não encontrado.");
        }

        var resultado = cliente.AtualizarDados(request.RazaoSocial, request.CodigoExterno, request.GrupoClienteId, request.RegimeTributario);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
