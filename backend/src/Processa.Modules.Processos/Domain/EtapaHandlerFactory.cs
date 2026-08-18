namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Resolve o IEtapaHandler certo por TipoEtapa (ver ADR-003). Os handlers concretos
/// são registrados via DI em Infrastructure; o novo tipo de etapa só exige um novo
/// handler + registro — nunca alterar os handlers existentes (Open/Closed).
/// </summary>
public sealed class EtapaHandlerFactory(IEnumerable<IEtapaHandler> handlers)
{
    public IEtapaHandler ObterHandler(TipoEtapa tipo) =>
        handlers.FirstOrDefault(h => h.Tipo == tipo)
            ?? throw new InvalidOperationException($"Nenhum handler registrado para o tipo de etapa {tipo}.");
}
