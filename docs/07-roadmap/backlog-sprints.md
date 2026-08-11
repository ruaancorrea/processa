# Backlog Detalhado por Sprint

Espelha o backlog rastreado no Jira (projeto `PROJ`) — 14 épicos, 13 sprints, 59 itens de trabalho detalhados. Referências `PROJ-N` apontam para a issue correspondente. Ver também [`../../backlog/README.md`](../../backlog/README.md) para o histórico de como esse backlog foi estruturado.

## Sprint 0 — Fundação Técnica
**Épico:** E1 · Fundação Técnica (`PROJ-1`) · **Sprint:** `PROJ-15`

| Item | Descrição |
|---|---|
| `PROJ-28` | Estruturar solução .NET em Clean Architecture (Api, Modules.*, Shared.Kernel) |
| `PROJ-29` | Configurar Docker Compose local (Postgres, Redis, RabbitMQ) com health checks |
| `PROJ-30` | Configurar CI no GitHub Actions (build, lint, testes, gate de cobertura) |
| `PROJ-31` | Criar testes de arquitetura (NetArchTest) validando regras de dependência do ADR-001 |
| `PROJ-32` | Bootstrap do frontend React + Vite + TypeScript |

**Pronto quando:** `docker compose up` sobe toda a stack local; CI verde em PR de exemplo; esqueleto de módulos sem violação de dependência.

## Sprint 1 — Identidade, Tenants e Controle de Acesso
**Épico:** E2 (`PROJ-2`) · **Sprint:** `PROJ-16`

| Item | Descrição |
|---|---|
| `PROJ-33` | Cadastro de tenant (escritório) e onboarding inicial |
| `PROJ-34` | Autenticação JWT (access token + refresh token) |
| `PROJ-35` | RBAC por papel (admin, gestor, analista) em todas as rotas da API |
| `PROJ-36` | Bloqueio de conta após tentativas de login falhas consecutivas |

**Pronto quando:** um tenant novo se cadastra, cria usuários com papéis diferentes, e cada papel só acessa o que lhe é permitido (validado por teste de integração).

## Sprint 2 — Cadastro de Clientes e Equipes
**Épico:** E3 (`PROJ-3`) · **Sprint:** `PROJ-17`

| Item | Descrição |
|---|---|
| `PROJ-37` | CRUD de equipes e membros com papel (gestor/analista) |
| `PROJ-38` | CRUD de clientes e grupos de clientes |
| `PROJ-39` | CRUD de contatos do cliente |
| `PROJ-40` | Vínculo responsável-cliente por equipe |

**Pronto quando:** admin cria equipe, adiciona membros, cadastra cliente com contatos e define responsáveis por equipe.

## Sprint 3 — Motor de Processos I: Configuração
**Épico:** E4 (`PROJ-4`) · **Sprint:** `PROJ-18`

| Item | Descrição |
|---|---|
| `PROJ-41` | CRUD de tipos de processo (modos de atribuição fixo/dinâmico/manual) |
| `PROJ-42` | Campos personalizados (8 tipos) por tipo de processo |
| `PROJ-43` | CRUD de fluxos com regra de fluxo padrão único por tipo de processo |
| `PROJ-44` | Permissões de início de demanda por perfil/usuário |

**Pronto quando:** gestor configura um tipo de processo completo (campos + fluxo) sem suporte técnico.

## Sprint 4 — Motor de Processos II: os 8 Tipos de Etapa
**Épico:** E5 (`PROJ-5`) · **Sprint:** `PROJ-19`

| Item | Descrição |
|---|---|
| `PROJ-45` | Etapa Comum e Etapa de Conclusão |
| `PROJ-46` | Etapa Condicional (desdobramento) com redirecionamento de fluxo |
| `PROJ-47` | Etapa Automatizada (chamada HTTP + validação de resposta) |
| `PROJ-48` | Etapa de Notificação e Etapa de Agendamento |
| `PROJ-49` | Etapa de Subprocesso com herança de campos personalizados do pai |
| `PROJ-50` | Etapa de União (fork/join) aguardando múltiplos desdobramentos |
| `PROJ-51` | Configuração de acesso por etapa (quem pode alterar/concluir) |

**Pronto quando:** todos os tipos de etapa funcionam ponta a ponta, incluindo o caso de fork/join.

## Sprint 5 — Execução de Demandas
**Épico:** E6 (`PROJ-6`) · **Sprint:** `PROJ-20`

| Item | Descrição |
|---|---|
| `PROJ-52` | Abertura de demanda por formulário |
| `PROJ-53` | Abertura de demanda por API e por subprocesso automático |
| `PROJ-54` | Atribuição de responsável nos 3 modos (fixo/dinâmico/manual) |
| `PROJ-55` | Progressão de etapas com histórico e temporização |
| `PROJ-56` | Comentários e anexos por execução de etapa |

**Pronto quando:** uma demanda percorre um fluxo completo do início à conclusão, com histórico auditável.

## Sprint 6 — Kanban e Painel Operacional
**Épico:** E7 (`PROJ-7`) · **Sprint:** `PROJ-21`

| Item | Descrição |
|---|---|
| `PROJ-57` | Board kanban com colunas configuráveis e drag-and-drop |
| `PROJ-58` | Visão em lista com filtros multicritério e ordenação cumulativa |
| `PROJ-59` | Seleção múltipla e edição em massa (responsável, prioridade, status) |
| `PROJ-60` | Atualização em tempo real via SignalR no kanban |

**Pronto quando:** a equipe trabalha visualmente ou em lista densa, com filtros e edição em massa.

## Sprint 7 — Motor de Regras e Automações
**Épico:** E8 (`PROJ-8`) · **Sprint:** `PROJ-22`

| Item | Descrição |
|---|---|
| `PROJ-61` | Modelagem de regra (trigger + condição + ação) e motor de avaliação assíncrono |
| `PROJ-62` | Regra de escalonamento por tarefa atrasada |
| `PROJ-63` | Regra de alerta de documento pendente perto do vencimento |
| `PROJ-64` | Regra de liberação automática da próxima etapa |

**Pronto quando:** admin cadastra uma regra sem código e ela dispara corretamente.

## Sprint 8 — Notificações Multicanal
**Épico:** E9 (`PROJ-9`) · **Sprint:** `PROJ-23`

| Item | Descrição |
|---|---|
| `PROJ-65` | Notificação interna em tempo real (SignalR + central de notificações) |
| `PROJ-66` | Integração de e-mail (Gmail API/SMTP) com editor de template e variáveis |
| `PROJ-67` | Integração WhatsApp (provedor externo) |
| `PROJ-68` | Momentos de disparo: ao entrar/sair, lembrete periódico, lembrete antes do prazo |

**Pronto quando:** notificação certa, no canal certo, para o destinatário certo, no momento certo.

## Sprint 9 — Documentos e Portal do Cliente
**Épico:** E10 (`PROJ-10`) · **Sprint:** `PROJ-24`

| Item | Descrição |
|---|---|
| `PROJ-69` | Upload e versionamento de documentos com categorias |
| `PROJ-70` | Fluxo de aprovação de documento |
| `PROJ-71` | Solicitação de documentos ao cliente |
| `PROJ-72` | Portal do cliente: autenticação própria e tela de pendências |
| `PROJ-73` | Portal do cliente: upload e histórico de documentos |

**Pronto quando:** cliente final acessa o portal, envia documento, escritório recebe e valida.

## Sprint 10 — Dashboards, Central de Pendências e Auditoria
**Épico:** E11 (`PROJ-11`) · **Sprint:** `PROJ-25`

| Item | Descrição |
|---|---|
| `PROJ-74` | Dashboard de desempenho por equipe |
| `PROJ-75` | Central de Pendências (atrasadas / vence hoje / vence semana / em dia) |
| `PROJ-76` | Log de auditoria imutável com filtros |
| `PROJ-77` | Exportação de relatórios (CSV/Excel) |

**Pronto quando:** gestor identifica em segundos onde estão os gargalos da equipe.

## Sprint 11 — Integrações Externas e API Pública
**Épico:** E12 (`PROJ-12`) · **Sprint:** `PROJ-26`

| Item | Descrição |
|---|---|
| `PROJ-78` | Tokens de API com escopos granulares |
| `PROJ-79` | Documentação OpenAPI/Swagger pública |
| `PROJ-80` | Rota de priorização de demanda via API |
| `PROJ-81` | Webhooks de saída para eventos de processo |

**Pronto quando:** sistema externo abre e prioriza demanda via API autenticada, respeitando escopos.

## Sprint 12 — Observabilidade, Segurança e Piloto
**Épico:** E13 (`PROJ-13`) · **Sprint:** `PROJ-27`

| Item | Descrição |
|---|---|
| `PROJ-82` | OpenTelemetry (tracing distribuído) + Serilog (logs estruturados) |
| `PROJ-83` | Health checks e rate limiting |
| `PROJ-84` | Revisão de segurança (checklist OWASP Top 10) |
| `PROJ-85` | Testes de carga e ajuste de performance |
| `PROJ-86` | Piloto com escritório real e estabilização pós-piloto |

**Pronto quando:** sistema em staging com observabilidade completa, checklist de segurança aprovado, piloto real iniciado.

---

## Backlog pós-MVP (não sprintado)

**Épico E14 · IA Assistiva** (`PROJ-14`) — resumo de pendências do cliente via IA, geração assistida de fluxo a partir de texto, assistente de consulta em linguagem natural. Deliberadamente fora dos 13 sprints do MVP — entra em planejamento de sprint somente após o piloto validar o core do produto (ver [roadmap](roadmap-mvp.md#pós-mvp-fase-2--não-bloqueia-o-piloto)).
