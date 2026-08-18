using FluentValidation;
using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Etapas;

/// <summary>PROJ-51 — quem além do responsável natural da demanda pode alterar/concluir esta etapa.</summary>
public sealed record DefinirConfiguracaoAcessoEtapaCommand(Guid EtapaId, List<Perfil>? PodeAlterar, List<Guid>? UsuarioIdsPodeAlterar)
    : IRequest<Result>;

public sealed class DefinirConfiguracaoAcessoEtapaCommandValidator : AbstractValidator<DefinirConfiguracaoAcessoEtapaCommand>
{
    public DefinirConfiguracaoAcessoEtapaCommandValidator()
    {
        RuleFor(x => x.EtapaId).NotEmpty();
        RuleForEach(x => x.PodeAlterar).IsInEnum();
    }
}

public sealed class DefinirConfiguracaoAcessoEtapaCommandHandler(
    IEtapaRepository etapaRepository, IVerificadorUsuario verificadorUsuario, IUnitOfWork unitOfWork)
    : IRequestHandler<DefinirConfiguracaoAcessoEtapaCommand, Result>
{
    public async Task<Result> Handle(DefinirConfiguracaoAcessoEtapaCommand request, CancellationToken cancellationToken)
    {
        var etapa = await etapaRepository.ObterPorIdAsync(request.EtapaId, cancellationToken);
        if (etapa is null)
            return Result.Failure("Etapa não encontrada.");

        foreach (var usuarioId in (request.UsuarioIdsPodeAlterar ?? []).Distinct())
        {
            if (!await verificadorUsuario.ExisteAsync(usuarioId, cancellationToken))
                return Result.Failure($"Usuário {usuarioId} não encontrado.");
        }

        etapa.DefinirConfiguracaoAcesso(ConfiguracaoAcessoEtapa.Criar(request.PodeAlterar, request.UsuarioIdsPodeAlterar));
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
