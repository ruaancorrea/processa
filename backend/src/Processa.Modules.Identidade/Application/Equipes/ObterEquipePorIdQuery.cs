using MediatR;
using Processa.Modules.Identidade.Application.Repositorios;
using Processa.Shared.Kernel;

namespace Processa.Modules.Identidade.Application.Equipes;

public sealed record MembroEquipeResumo(Guid UsuarioId, string NomeUsuario, string Papel);

public sealed record EquipeDetalhe(Guid Id, string Nome, string? Descricao, bool Ativa, List<MembroEquipeResumo> Membros);

public sealed record ObterEquipePorIdQuery(Guid EquipeId) : IRequest<Result<EquipeDetalhe>>;

public sealed class ObterEquipePorIdQueryHandler(
    IEquipeRepository equipeRepository, IMembroEquipeRepository membroEquipeRepository, IUsuarioRepository usuarioRepository)
    : IRequestHandler<ObterEquipePorIdQuery, Result<EquipeDetalhe>>
{
    public async Task<Result<EquipeDetalhe>> Handle(ObterEquipePorIdQuery request, CancellationToken cancellationToken)
    {
        var equipe = await equipeRepository.ObterPorIdAsync(request.EquipeId, cancellationToken);
        if (equipe is null)
            return Result.Failure<EquipeDetalhe>("Equipe não encontrada.");

        var membros = await membroEquipeRepository.ListarPorEquipeAsync(request.EquipeId, cancellationToken);

        var membrosResumo = new List<MembroEquipeResumo>();
        foreach (var membro in membros)
        {
            var usuario = await usuarioRepository.ObterPorIdAsync(membro.UsuarioId, cancellationToken);
            membrosResumo.Add(new MembroEquipeResumo(membro.UsuarioId, usuario?.Nome ?? "(usuário removido)", membro.Papel.ToString()));
        }

        return Result.Success(new EquipeDetalhe(equipe.Id, equipe.Nome, equipe.Descricao, equipe.Ativa, membrosResumo));
    }
}
