using MediatR;
using Processa.Modules.Processos.Application.Demandas;
using Processa.Modules.Processos.Application.Repositorios;
using Processa.Modules.Processos.Domain;

namespace Processa.Modules.Processos.Infrastructure;

/// <summary>Herda cliente (sempre) e responsável (se configurado) do processo pai (PROJ-49) — reaproveita CriarDemandaCommand via ISender, não duplica a regra de criação.</summary>
public sealed class CriadorSubprocesso(ISender sender, IDemandaRepository demandaRepository) : ICriadorSubprocesso
{
    public async Task<Guid> CriarAsync(
        Guid tenantId, Guid demandaPaiId, Guid tipoProcessoFilhoId, bool herdarResponsavel, CancellationToken cancellationToken = default)
    {
        var demandaPai = await demandaRepository.ObterPorIdAsync(demandaPaiId, cancellationToken)
            ?? throw new InvalidOperationException($"Demanda pai {demandaPaiId} não encontrada ao tentar abrir subprocesso.");

        var comando = new CriarDemandaCommand(
            tipoProcessoFilhoId,
            demandaPai.ClienteId,
            herdarResponsavel ? demandaPai.ResponsavelId : null,
            demandaPai.Prioridade,
            null,
            null,
            demandaPaiId);

        var resultado = await sender.Send(comando, cancellationToken);
        if (resultado.IsFailure)
            throw new InvalidOperationException($"Falha ao abrir subprocesso: {resultado.Error}");

        return resultado.Value;
    }
}
