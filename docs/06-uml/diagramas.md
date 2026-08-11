# Diagramas UML

Complementa o [modelo de domínio](../03-modelagem/modelo-de-dominio.md) com a visão de classes e as sequências dos fluxos mais críticos do motor de processos.

## 1. Diagrama de classes — núcleo do motor de processos

```mermaid
classDiagram
    class TipoProcesso {
        +Guid Id
        +Guid TenantId
        +Guid EquipeId
        +string Nome
        +bool ResponsavelObrigatorio
        +ModoAtribuicao ModoAtribuicao
        +List~CampoPersonalizado~ Campos
        +List~Fluxo~ Fluxos
        +AdicionarFluxo(fluxo) Fluxo
        +DefinirFluxoPadrao(fluxoId) void
    }

    class Fluxo {
        +Guid Id
        +string Nome
        +bool FluxoPadrao
        +List~Etapa~ Etapas
        +AdicionarEtapa(etapa) void
        +ReordenarEtapas(ordem) void
    }

    class Etapa {
        +Guid Id
        +string Nome
        +TipoEtapa Tipo
        +int Ordem
        +JsonDocument Configuracao
        +ConfiguracaoAcesso Acesso
    }

    class Demanda {
        +Guid Id
        +Guid TipoProcessoId
        +Guid ClienteId
        +Guid? ResponsavelId
        +StatusDemanda Status
        +Prioridade Prioridade
        +Guid? EtapaAtualId
        +Guid? DemandaPaiId
        +Avancar(resultado) void
        +Cancelar() void
        +Reabrir() void
        +AtribuirResponsavel(usuarioId, origem) void
    }

    class ExecucaoEtapa {
        +Guid Id
        +Guid DemandaId
        +Guid EtapaId
        +StatusExecucao Status
        +DateTime IniciadoEm
        +DateTime? ConcluidoEm
        +Concluir(dados) void
        +Travar(motivo) void
        +AdicionarComentario(texto, autor) void
    }

    class IEtapaHandler {
        <<interface>>
        +TipoEtapa Tipo
        +Executar(execucao, config) Task~ResultadoExecucao~
    }

    class EtapaComumHandler
    class EtapaCondicionalHandler
    class EtapaAutomatizadaHandler
    class EtapaSubprocessoHandler
    class EtapaUniaoHandler

    class RegraAutomacao {
        +Guid Id
        +TriggerRegra Trigger
        +JsonDocument Condicao
        +JsonDocument Acao
        +Avaliar(contexto) bool
    }

    TipoProcesso "1" *-- "many" Fluxo
    Fluxo "1" *-- "many" Etapa
    TipoProcesso "1" --> "many" Demanda : instancia
    Demanda "1" *-- "many" ExecucaoEtapa
    Etapa "1" --> "many" ExecucaoEtapa : define
    IEtapaHandler <|.. EtapaComumHandler
    IEtapaHandler <|.. EtapaCondicionalHandler
    IEtapaHandler <|.. EtapaAutomatizadaHandler
    IEtapaHandler <|.. EtapaSubprocessoHandler
    IEtapaHandler <|.. EtapaUniaoHandler
    Demanda "1" --> "0..1" Demanda : DemandaPaiId
    TipoProcesso "1" --> "many" RegraAutomacao
```

## 2. Sequência — abertura de demanda por formulário

```mermaid
sequenceDiagram
    actor U as Usuário (Analista/Gestor)
    participant API as DemandasController
    participant APP as AbrirDemandaCommandHandler
    participant DOM as Demanda (Domain)
    participant REPO as IDemandaRepository
    participant MQ as RabbitMQ

    U->>API: POST /api/v1/demandas {tipoProcessoId, clienteId, campos}
    API->>APP: Send(AbrirDemandaCommand)
    APP->>APP: Valida permissão de início (perfil x tipo de processo)
    APP->>DOM: Demanda.Abrir(tipoProcesso, cliente, campos)
    DOM->>DOM: Aplica modo de atribuição (fixo/dinâmico/manual)
    DOM->>DOM: Define EtapaAtual = primeira etapa do fluxo padrão
    APP->>REPO: Add(demanda) + SaveChanges (transação)
    APP->>MQ: Publica DemandaAbertaEvent
    APP-->>API: DemandaDto
    API-->>U: 201 Created
    MQ->>MQ: (assíncrono) Modules.Notificacoes consome evento<br/>e dispara notificações "ao_entrar" da 1ª etapa
```

## 3. Sequência — conclusão de etapa com desdobramento condicional

```mermaid
sequenceDiagram
    actor U as Usuário
    participant API as ExecucaoEtapasController
    participant APP as ConcluirEtapaCommandHandler
    participant FACT as EtapaHandlerFactory
    participant H as EtapaCondicionalHandler
    participant DOM as Demanda (Domain)
    participant DB as PostgreSQL

    U->>API: POST /api/v1/execucao-etapas/{id}/concluir {resultado}
    API->>APP: Send(ConcluirEtapaCommand)
    APP->>FACT: Resolver(TipoEtapa.Condicional)
    FACT-->>APP: EtapaCondicionalHandler
    APP->>H: Executar(execucao, configuracao)
    H->>H: Avalia campo configurado contra as opções
    H-->>APP: ResultadoExecucao{proximaEtapaId}
    APP->>DOM: Demanda.Avancar(resultado)
    DOM->>DOM: Valida transição (máquina de estados)
    DOM->>DOM: Atualiza EtapaAtualId
    APP->>DB: Persiste ExecucaoEtapa concluída +<br/>nova ExecucaoEtapa da próxima etapa +<br/>HistoricoExecucaoEtapa
    APP-->>API: OK
    API-->>U: 200 OK
```

## 4. Sequência — motor de regras (escalonamento assíncrono)

```mermaid
sequenceDiagram
    participant SCH as Scheduler (Quartz.NET)
    participant AVAL as AvaliadorDeRegras
    participant DB as PostgreSQL
    participant MQ as RabbitMQ
    participant W as NotificacaoWorker

    loop A cada N minutos
        SCH->>AVAL: AvaliarRegrasAtivas()
        AVAL->>DB: Busca execucoes_etapa em_andamento<br/>com iniciado_em > limiar
        DB-->>AVAL: Lista de execuções candidatas
        AVAL->>AVAL: Avalia condição de cada RegraAutomacao aplicável
        alt Condição satisfeita
            AVAL->>DB: Registra evento em historico_execucao_etapa<br/>(origem: regra automática)
            AVAL->>MQ: Publica RegraDisparadaEvent{acao: escalonar}
            MQ->>W: Consome evento
            W->>W: Envia notificação ao gestor
            W->>DB: Eleva prioridade da demanda
        end
    end
```

Estes diagramas descrevem o comportamento *pretendido* do sistema (design antes da implementação) — servem como especificação para o desenvolvimento a partir do Sprint 3, conforme o [roadmap](../07-roadmap/roadmap-mvp.md).
