using MediatR;
using Processa.Modules.Clientes.Application.Repositorios;
using Processa.Modules.Clientes.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Clientes.Application.Responsaveis;

/// <summary>
/// Verifica se o usuário é membro da equipe via IVerificadorMembroEquipe (contrato
/// em Shared.Kernel, implementado em Identidade) — nunca por referência direta ao
/// assembly de Identidade, nem Domain/Infrastructure nem Application (ver
/// .claude/architecture.md e .faf/decisions.faf). Inversão de dependência na
/// fronteira do módulo, não acoplamento de compilação entre módulos.
/// </summary>
public sealed record AdicionarResponsavelCommand(Guid ClienteId, Guid EquipeId, Guid UsuarioId) : IRequest<Result<Guid>>;

public sealed class AdicionarResponsavelCommandHandler(
    IResponsavelClienteRepository responsavelClienteRepository,
    IClienteRepository clienteRepository,
    IVerificadorMembroEquipe verificadorMembroEquipe,
    ITenantContext tenantContext,
    IUnitOfWork unitOfWork) : IRequestHandler<AdicionarResponsavelCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AdicionarResponsavelCommand request, CancellationToken cancellationToken)
    {
        var cliente = await clienteRepository.ObterPorIdAsync(request.ClienteId, cancellationToken);
        if (cliente is null)
            return Result.Failure<Guid>("Cliente não encontrado.");

        var ehMembro = await verificadorMembroEquipe.EhMembroAsync(request.EquipeId, request.UsuarioId, cancellationToken);
        if (!ehMembro)
            return Result.Failure<Guid>("A equipe informada não existe ou o usuário não é membro dela.");

        var existente = await responsavelClienteRepository.ObterAsync(request.ClienteId, request.EquipeId, request.UsuarioId, cancellationToken);
        if (existente is not null)
        {
            if (existente.EstaAtivo)
                return Result.Failure<Guid>("Este usuário já é responsável por este cliente nesta equipe.");

            // Reativa o vínculo soft-deletado em vez de criar um novo (evita violar
            // o índice único parcial documentado em docs/03-modelagem/modelo-de-dados.md).
            existente.Reativar();
            await unitOfWork.SalvarAsync(cancellationToken);
            return Result.Success(existente.Id);
        }

        var responsavelResult = ResponsavelCliente.Criar(tenantContext.TenantId, request.ClienteId, request.EquipeId, request.UsuarioId);
        if (responsavelResult.IsFailure)
            return Result.Failure<Guid>(responsavelResult.Error!);

        await responsavelClienteRepository.AddAsync(responsavelResult.Value, cancellationToken);
        await unitOfWork.SalvarAsync(cancellationToken);

        return Result.Success(responsavelResult.Value.Id);
    }
}
