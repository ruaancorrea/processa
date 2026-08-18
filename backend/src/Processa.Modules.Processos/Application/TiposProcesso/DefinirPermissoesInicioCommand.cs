using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record DefinirPermissoesInicioCommand(Guid TipoProcessoId, List<Perfil>? Perfis, List<Guid>? UsuarioIds) : IRequest<Result>;

public sealed class DefinirPermissoesInicioCommandHandler(
    ITipoProcessoRepository tipoProcessoRepository,
    IVerificadorUsuario verificadorUsuario,
    IUnitOfWork unitOfWork) : IRequestHandler<DefinirPermissoesInicioCommand, Result>
{
    public async Task<Result> Handle(DefinirPermissoesInicioCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure("Tipo de processo não encontrado.");

        // Perfis/UsuarioIds nulos (cliente manda null em vez de [] pra "limpar
        // permissões") não podem estourar NullReferenceException aqui — achado real
        // de revisão, mesma classe de defesa já aplicada em CampoPersonalizado.Criar.
        foreach (var usuarioId in (request.UsuarioIds ?? []).Distinct())
        {
            if (!await verificadorUsuario.ExisteAsync(usuarioId, cancellationToken))
                return Result.Failure($"Usuário {usuarioId} não encontrado.");
        }

        var permissoes = PermissoesInicio.Criar(request.Perfis, request.UsuarioIds);
        tipoProcesso.DefinirPermissoesInicio(permissoes);
        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
