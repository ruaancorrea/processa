using MediatR;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;
using Processa.Shared.Kernel;

namespace Processa.Modules.Processos.Application.Demandas;

/// <summary>Marca uma etapa Comum (ou qualquer execução aberta) como concluída manualmente e dispara o orquestrador (PROJ-55).</summary>
public sealed record ConcluirExecucaoEtapaCommand(Guid ExecucaoEtapaId) : IRequest<Result>;

public sealed class ConcluirExecucaoEtapaCommandHandler(
    IExecucaoEtapaRepository execucaoEtapaRepository,
    IDemandaRepository demandaRepository,
    IHistoricoExecucaoEtapaRepository historicoRepository,
    OrquestradorExecucao orquestrador,
    IUsuarioContext usuarioContext,
    IUnitOfWork unitOfWork) : IRequestHandler<ConcluirExecucaoEtapaCommand, Result>
{
    public async Task<Result> Handle(ConcluirExecucaoEtapaCommand request, CancellationToken cancellationToken)
    {
        var execucao = await execucaoEtapaRepository.ObterPorIdAsync(request.ExecucaoEtapaId, cancellationToken);
        if (execucao is null)
            return Result.Failure("Execução não encontrada.");

        if (!AutorizacaoExecucao.PodeInteragir(usuarioContext, execucao))
            return Result.Failure("Só o responsável, um gestor ou um admin pode concluir esta etapa.");

        var demanda = await demandaRepository.ObterPorIdAsync(execucao.DemandaId, cancellationToken);
        if (demanda is null)
            return Result.Failure("Demanda não encontrada.");

        var resultado = execucao.Concluir();
        if (resultado.IsFailure)
            return resultado;

        var historico = HistoricoExecucaoEtapa.Registrar(
            execucao.TenantId, execucao.Id, TipoEventoHistorico.StatusAlterado, "{\"para\":\"Concluida\"}", usuarioContext.UsuarioId);
        if (historico.IsSuccess)
            await historicoRepository.AddAsync(historico.Value, cancellationToken);

        await unitOfWork.SalvarAsync(cancellationToken);

        await orquestrador.ContinuarAposConclusaoManualAsync(demanda, execucao, cancellationToken);

        return Result.Success();
    }
}
