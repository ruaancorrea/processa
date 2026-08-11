# Catálogo de Casos de Uso

Referência cruzada com os módulos descritos em [`requisitos-funcionais.md`](requisitos-funcionais.md) e com os épicos do backlog em [`../07-roadmap/backlog-sprints.md`](../07-roadmap/backlog-sprints.md).

| ID | Caso de Uso | Ator Principal | Módulo |
|----|-------------|-----------------|--------|
| UC-01 | Cadastrar novo tenant (escritório) | Administrador | Tenants e Identidade |
| UC-02 | Gerenciar usuários (CRUD) | Administrador | Equipes e Usuários |
| UC-03 | Gerenciar equipes e membros | Administrador | Equipes e Usuários |
| UC-04 | Gerenciar clientes e grupos | Admin, Gestor, Analista | Clientes |
| UC-05 | Gerenciar contatos do cliente | Admin, Gestor, Analista | Clientes |
| UC-06 | Vincular responsável a cliente por equipe | Admin, Gestor | Clientes |
| UC-07 | Configurar tipo de processo | Admin, Gestor | Configuração de Processos |
| UC-08 | Gerenciar campos personalizados | Admin, Gestor | Configuração de Processos |
| UC-09 | Configurar fluxos | Admin, Gestor | Configuração de Processos |
| UC-10 | Configurar etapa Comum | Admin, Gestor | Motor de Processos |
| UC-11 | Configurar etapa Condicional | Admin, Gestor | Motor de Processos |
| UC-12 | Configurar etapa Automatizada | Admin, Gestor | Motor de Processos |
| UC-13 | Configurar etapa de Notificação | Admin, Gestor | Motor de Processos |
| UC-14 | Configurar etapa de Agendamento | Admin, Gestor | Motor de Processos |
| UC-15 | Configurar etapa de Subprocesso | Admin, Gestor | Motor de Processos |
| UC-16 | Configurar etapa de Conclusão | Admin, Gestor | Motor de Processos |
| UC-17 | Configurar etapa de União (fork/join) | Admin, Gestor | Motor de Processos |
| UC-18 | Configurar acesso por etapa | Admin, Gestor | Motor de Processos |
| UC-19 | Iniciar demanda por formulário | Analista, Gestor, Admin | Execução de Demandas |
| UC-20 | Iniciar demanda por API | Sistema Externo | Execução de Demandas |
| UC-21 | Iniciar demanda por subprocesso | Sistema (automático) | Execução de Demandas |
| UC-22 | Atribuir responsável (manual) | Gestor | Execução de Demandas |
| UC-23 | Atribuir responsável (automático — fixo/dinâmico) | Sistema | Execução de Demandas |
| UC-24 | Executar etapa (qualquer tipo) | Analista, Sistema | Execução de Demandas |
| UC-25 | Adicionar comentário/anexo à execução de etapa | Responsável, Gestor, Admin | Execução de Demandas |
| UC-26 | Cancelar / reabrir demanda | Gestor, Admin | Execução de Demandas |
| UC-27 | Visualizar histórico completo de etapa | Gestor, Admin, Analista | Execução de Demandas |
| UC-28 | Visualizar painel de demandas (lista) | Gestor, Analista | Kanban e Painel |
| UC-29 | Visualizar e mover cards no kanban | Gestor, Analista | Kanban e Painel |
| UC-30 | Filtrar e ordenar demandas com multicritério | Gestor, Analista | Kanban e Painel |
| UC-31 | Editar demandas em massa | Gestor, Admin | Kanban e Painel |
| UC-32 | Configurar notificação de etapa (canal/destinatário/momento) | Admin, Gestor | Notificações |
| UC-33 | Configurar múltiplas notificações por etapa | Admin, Gestor | Notificações |
| UC-34 | Editar corpo de notificação (HTML/variáveis) | Admin, Gestor | Notificações |
| UC-35 | Receber notificação (qualquer canal) | Usuário interno, Cliente | Notificações |
| UC-36 | Configurar regra de automação | Admin | Motor de Regras |
| UC-37 | Regra disparar escalonamento por atraso | Sistema (automático) | Motor de Regras |
| UC-38 | Regra disparar alerta de documento pendente | Sistema (automático) | Motor de Regras |
| UC-39 | Upload e versionamento de documento | Analista, Gestor | Documentos |
| UC-40 | Aprovar/rejeitar documento | Gestor, Admin | Documentos |
| UC-41 | Solicitar documento ao cliente | Analista, Gestor | Documentos |
| UC-42 | Login no portal do cliente | Cliente | Portal do Cliente |
| UC-43 | Visualizar pendências no portal | Cliente | Portal do Cliente |
| UC-44 | Enviar documento pelo portal | Cliente | Portal do Cliente |
| UC-45 | Visualizar dashboard de equipe | Gestor, Admin | Dashboards e Auditoria |
| UC-46 | Consultar Central de Pendências | Gestor, Admin | Dashboards e Auditoria |
| UC-47 | Consultar log de auditoria | Administrador | Dashboards e Auditoria |
| UC-48 | Emitir / revogar token de API | Admin, Gestor | Integrações |
| UC-49 | Priorizar demanda via API | Sistema Externo | Integrações |
| UC-50 | Configurar integração externa (Gmail, Calendar, WhatsApp) | Administrador | Integrações |
| UC-51 | Configurar webhook de saída | Administrador | Integrações |
| UC-52 | Consultar resumo de pendências do cliente via IA *(Fase 2)* | Gestor, Analista | IA Assistiva |
| UC-53 | Gerar sugestão de fluxo a partir de texto via IA *(Fase 2)* | Admin, Gestor | IA Assistiva |
| UC-54 | Consultar dados via linguagem natural (IA) *(Fase 2)* | Gestor, Admin | IA Assistiva |

## Casos de uso detalhados (amostra representativa)

### UC-19 — Iniciar demanda por formulário

- **Ator principal:** Analista, Gestor ou Administrador com permissão de início configurada no tipo de processo.
- **Pré-condições:** Tipo de processo ativo, com ao menos um fluxo configurado; cliente cadastrado e ativo.
- **Fluxo principal:**
  1. Usuário seleciona "Nova demanda" e escolhe o tipo de processo.
  2. Sistema apresenta formulário com campos padrão (cliente obrigatório; responsável, prioridade, data_fim_prevista opcionais conforme configuração) e campos personalizados do tipo.
  3. Usuário preenche e submete.
  4. Sistema cria a demanda com `fluxo_ativo` = fluxo padrão do tipo de processo, `etapa_atual` = primeira etapa do fluxo, e aplica o modo de atribuição de responsável configurado.
  5. Sistema dispara notificações `ao_entrar` configuradas na primeira etapa.
- **Fluxos alternativos:** se `responsavel_obrigatorio=false` e nenhum responsável for informado, a demanda nasce com status `sem_responsavel` e o gestor da equipe recebe notificação.
- **Pós-condição:** demanda visível no painel/kanban da equipe, com histórico de criação registrado em auditoria.

### UC-37 — Regra disparar escalonamento por atraso

- **Ator principal:** Sistema (worker assíncrono do motor de regras).
- **Trigger:** avaliação periódica (Celery/Hangfire-equivalente no .NET, via `IHostedService`/`Quartz.NET`) verifica demandas cuja etapa atual excede o tempo configurado na regra.
- **Fluxo principal:**
  1. Worker identifica execução de etapa com `iniciado_em` mais antigo que o limiar da regra e status ainda `em_andamento`.
  2. Motor avalia a condição (ex: "há mais de 3 dias na mesma etapa").
  3. Ação disparada: eleva prioridade da demanda, notifica o gestor da equipe, registra o evento em `historico_execucao_etapa` com origem "regra automática".
- **Pós-condição:** demanda aparece destacada na Central de Pendências e no dashboard do gestor.

### UC-44 — Enviar documento pelo portal

- **Ator principal:** Cliente (usuário externo autenticado no portal).
- **Pré-condições:** Cliente possui uma solicitação de documento pendente vinculada a uma etapa em andamento.
- **Fluxo principal:**
  1. Cliente acessa o portal, vê a pendência na tela inicial.
  2. Faz upload do arquivo solicitado (validação de tipo/tamanho no frontend e no backend).
  3. Sistema armazena o documento (object storage), vincula à solicitação, marca como recebido.
  4. Se configurado, a etapa correspondente avança automaticamente (ex: de "Aguardando Documentos" para "Conferir Documentos").
  5. Sistema notifica o responsável interno sobre o recebimento.
- **Pós-condição:** documento disponível para o time interno, com registro de quem enviou e quando, visível no histórico da demanda.
