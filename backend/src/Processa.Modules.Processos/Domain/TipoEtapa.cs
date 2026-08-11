namespace Processa.Modules.Processos.Domain;

/// <summary>
/// Os 8 comportamentos de etapa suportados pelo motor de processos.
/// Ver docs/01-requisitos/requisitos-funcionais.md#6 e
/// docs/02-arquitetura/decisoes/adr-003-motor-de-processos-state-machine.md
/// </summary>
public enum TipoEtapa
{
    Comum,
    Condicional,
    Automatizada,
    Notificacao,
    Agendamento,
    Subprocesso,
    Conclusao,
    Uniao,
}
