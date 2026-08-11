# ADR-003 — Motor de Processos como Máquina de Estados Explícita

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O núcleo do produto é o motor de processos: 8 tipos de etapa com comportamentos distintos, incluindo desvio condicional de fluxo e fork/join (etapa de União). Esse comportamento não pode ser um `if/else` espalhado em serviços — precisa ser modelado como estado explícito e transições auditáveis.

## Decisão

Cada `Demanda` (instância de processo) e cada `ExecucaoEtapa` têm um **status modelado como enum de domínio com transições explícitas**, implementado como uma máquina de estados dentro da entidade de domínio (`Demanda.Avancar()`, `ExecucaoEtapa.Concluir()`, etc.) — não como uma biblioteca de state machine genérica de terceiros, para manter o modelo simples e testável sem dependência externa.

Cada tipo de etapa implementa uma interface de domínio `IEtapaHandler` (Strategy pattern) responsável por decidir a próxima transição:

```csharp
public interface IEtapaHandler
{
    TipoEtapa Tipo { get; }
    Task<ResultadoExecucao> Executar(ExecucaoEtapa execucao, ConfiguracaoEtapa config);
}
```

Handlers concretos: `EtapaComumHandler`, `EtapaCondicionalHandler`, `EtapaAutomatizadaHandler`, `EtapaNotificacaoHandler`, `EtapaAgendamentoHandler`, `EtapaSubprocessoHandler`, `EtapaConclusaoHandler`, `EtapaUniaoHandler`. Um `EtapaHandlerFactory` resolve o handler correto por `TipoEtapa`.

Toda transição de estado gera um `DomainEvent` (`EtapaConcluidaEvent`, `EtapaTravadaEvent`, `DemandaConcluidaEvent`...) publicado via MediatR in-process e, quando relevante para outros módulos ou workers, também no barramento RabbitMQ (ver [ADR-004](adr-004-mensageria-rabbitmq.md)).

## Alternativas consideradas

- **Workflow engine de terceiros (ex: Elsa Workflows, Temporal):** avaliado e descartado para o MVP — resolve orquestração genérica, mas adiciona uma dependência pesada e uma curva de aprendizado para um domínio que já é bem compreendido e specificado (8 tipos fechados de etapa, não um DSL genérico de workflow). Reavaliar se o catálogo de tipos de etapa crescer para um workflow verdadeiramente arbitrário definido pelo usuário.
- **Status como string livre:** rejeitado — impossibilita validação de transição em tempo de compilação/teste e convida a estados inconsistentes.

## Consequências

- Toda transição é testável unitariamente por handler, isolado do restante do sistema.
- Fork/join (etapa de União) exige rastrear múltiplos sub-fluxos filhos por demanda — modelado via `processo_pai_id` auto-referencial e uma tabela de acompanhamento dos desdobramentos aguardados (ver [modelo de dados](../../03-modelagem/modelo-de-dados.md)).
- Novo tipo de etapa = novo handler + registro na factory — extensão sem alterar os handlers existentes (Open/Closed Principle).
