using FluentValidation;
using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Grupos;

public sealed record CriarGrupoClienteCommand(string Nome, string? Descricao) : IRequest<Result<Guid>>;

public sealed class CriarGrupoClienteCommandValidator : AbstractValidator<CriarGrupoClienteCommand>
{
    public CriarGrupoClienteCommandValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public sealed class CriarGrupoClienteCommandHandler(
    IGrupoClienteRepository grupoClienteRepository, ITenantContext tenantContext, IUnitOfWork unitOfWork)
    : IRequestHandler<CriarGrupoClienteCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CriarGrupoClienteCommand request, CancellationToken cancellationToken)
    {
        var grupoResult = GrupoCliente.Criar(tenantContext.TenantId, request.Nome, request.Descricao);
        if (grupoResult.IsFailure)
            return Result.Failure<Guid>(grupoResult.Error!);

        await grupoClienteRepository.AddAsync(grupoResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(grupoResult.Value.Id);
    }
}
