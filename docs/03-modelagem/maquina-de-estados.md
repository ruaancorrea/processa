# Máquina de Estados

## 1. Status da Demanda

```mermaid
stateDiagram-v2
    [*] --> sem_responsavel: modo_atribuicao=manual<br/>e responsavel_obrigatorio=false
    [*] --> pendente: responsável atribuído<br/>na criação

    sem_responsavel --> pendente: gestor atribui responsável
    pendente --> em_andamento: responsável inicia<br/>a primeira etapa
    em_andamento --> em_andamento: avança de etapa em etapa
    em_andamento --> concluido: etapa de Conclusão atingida
    em_andamento --> cancelado: gestor/admin cancela

    pendente --> cancelado: gestor/admin cancela
    sem_responsavel --> cancelado: gestor/admin cancela

    concluido --> em_andamento: reabertura explícita<br/>(admin, auditada)
```

## 2. Status da Execução de Etapa

```mermaid
stateDiagram-v2
    [*] --> pendente: etapa criada ao entrar<br/>no fluxo
    pendente --> em_andamento: responsável começa<br/>a trabalhar (ou automático)
    em_andamento --> concluida: ação de conclusão<br/>(manual ou automática)
    em_andamento --> erro: falha em etapa<br/>Automatizada
    em_andamento --> aguardando: etapa de Subprocesso<br/>ou União aguardando
    aguardando --> concluida: subprocesso concluído /<br/>todos os desdobramentos concluídos
    erro --> em_andamento: correção manual<br/>e nova tentativa
    pendente --> pulada: etapa Condicional<br/>redireciona o fluxo
    concluida --> em_andamento: reabertura de etapa<br/>(auditada)
```

## 3. Transições por tipo de etapa

| Tipo de etapa | Como sai de `em_andamento` |
|---|---|
| Comum | Responsável marca manualmente como concluída ou não concluída |
| Condicional | Sistema avalia o campo configurado e decide automaticamente: próxima etapa, volta, ou outro fluxo |
| Automatizada | Resposta HTTP validada (status code ou campo JSON) → `concluida`; falha ou timeout → `erro` (trava) |
| Notificação | Disparo confirmado → `concluida` automaticamente |
| Agendamento | Modo standalone: conclusão automática na data/hora definida (via Scheduler). Modo Calendar: manual ou pós-horário do evento |
| Subprocesso | `aguardando` até a subdemanda atingir `concluido`; então `concluida` automaticamente |
| Conclusão | Ao ser atingida, marca a `Demanda` inteira como `concluido` |
| União | `aguardando` até todos os `desdobramentos_aguardados` vinculados terem `concluido=true` |

## 4. Diagrama de fork/join (etapa de União)

```mermaid
graph TB
    Cond["Etapa Condicional<br/>(desdobramento)"]
    Cond -->|opção A| SubA["Sub-fluxo A"]
    Cond -->|opção B| SubB["Sub-fluxo B"]
    SubA --> Uniao["Etapa de União<br/>status: aguardando"]
    SubB --> Uniao
    Uniao -->|todos concluídos| Proxima["Próxima etapa<br/>do fluxo principal"]
```

Toda transição acima é validada por teste unitário do respectivo `IEtapaHandler` (ver [ADR-003](../02-arquitetura/decisoes/adr-003-motor-de-processos-state-machine.md)) — nenhuma transição de estado é aceita fora das regras descritas aqui; tentativas inválidas retornam erro de domínio (`InvalidTransitionException`), nunca falham silenciosamente.
