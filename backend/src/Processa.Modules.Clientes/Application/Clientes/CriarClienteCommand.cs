using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Clientes;

public sealed record CriarClienteCommand(
    string RazaoSocial,
    string Cnpj,
    string? CodigoExterno,
    Guid? GrupoClienteId,
    RegimeTributario RegimeTributario,
    DateOnly DataEntrada) : IRequest<Result<Guid>>;

public sealed class CriarClienteCommandHandler(
    IClienteRepository clienteRepository,
    IGrupoClienteRepository grupoClienteRepository,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<CriarClienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarClienteCommand request, CancellationToken cancellationToken)
    {
        var clienteResult = Cliente.Criar(
            tenantContext.TenantId, request.RazaoSocial, request.Cnpj, request.CodigoExterno,
            request.GrupoClienteId, request.RegimeTributario, request.DataEntrada);
        if (clienteResult.IsFailure)
            return Result.Failure<Guid>(clienteResult.Error!);

        if (await clienteRepository.ExisteCnpjAsync(clienteResult.Value.Cnpj, cancellationToken))
            return Result.Failure<Guid>("Já existe um cliente cadastrado com este CNPJ.");

        if (request.GrupoClienteId is { } grupoId)
        {
            var grupo = await grupoClienteRepository.ObterPorIdAsync(grupoId, cancellationToken);
            if (grupo is null)
                return Result.Failure<Guid>("Grupo de clientes não encontrado.");
        }

        await clienteRepository.AddAsync(clienteResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(clienteResult.Value.Id);
    }
}
