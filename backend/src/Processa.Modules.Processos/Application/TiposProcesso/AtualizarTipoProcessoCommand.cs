using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.TiposProcesso;

public sealed record AtualizarTipoProcessoCommand(
    Guid TipoProcessoId,
    string Nome,
    string? Descricao,
    bool ResponsavelObrigatorio,
    ModoAtribuicao ModoAtribuicao,
    Guid? ResponsavelFixoId) : IRequest<Result>;

public sealed class AtualizarTipoProcessoCommandHandler(
    ITipoProcessoRepository tipoProcessoRepository,
    IVerificadorMembroEquipe verificadorMembroEquipe,
    IUnitOfWork unitOfWork) : IRequestHandler<AtualizarTipoProcessoCommand, Result>
{
    public async Task<Result> Handle(AtualizarTipoProcessoCommand request, CancellationToken cancellationToken)
    {
        var tipoProcesso = await tipoProcessoRepository.ObterPorIdAsync(request.TipoProcessoId, cancellationToken);
        if (tipoProcesso is null)
            return Result.Failure("Tipo de processo não encontrado.");

        if (request.ModoAtribuicao == ModoAtribuicao.Fixo)
        {
            if (request.ResponsavelFixoId is not { } responsavelFixoId
                || !await verificadorMembroEquipe.EhMembroAsync(tipoProcesso.EquipeId, responsavelFixoId, cancellationToken))
                return Result.Failure("O responsável fixo precisa ser membro da equipe do tipo de processo.");
        }

        var resultado = tipoProcesso.AtualizarDados(
            request.Nome, request.Descricao, request.ResponsavelObrigatorio, request.ModoAtribuicao, request.ResponsavelFixoId);
        if (resultado.IsFailure)
            return resultado;

        await unitOfWork.SalvarAsync(cancellationToken);
        return Result.Success();
    }
}
