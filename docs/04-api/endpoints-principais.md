# Catálogo de Endpoints Principais

Convenções gerais em [`convencoes-api.md`](convencoes-api.md). Lista não-exaustiva — a fonte de verdade em tempo de execução é o OpenAPI gerado a partir do código.

## Identidade e Tenants
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| POST | `/api/v1/tenants` | Cadastro de novo escritório (onboarding) | público |
| POST | `/api/v1/auth/login` | Login, retorna access + refresh token | público |
| POST | `/api/v1/auth/refresh` | Renova access token | refresh token válido |
| POST | `/api/v1/auth/logout` | Revoga refresh token atual | autenticado |
| GET/POST/PUT | `/api/v1/usuarios` | CRUD de usuários do tenant | admin |

## Equipes e Clientes
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET/POST/PUT | `/api/v1/equipes` | CRUD de equipes | admin |
| POST | `/api/v1/equipes/{id}/membros` | Adiciona membro com papel | admin |
| GET/POST/PUT | `/api/v1/clientes` | CRUD de clientes | admin, gestor, `clientes:write` |
| GET/POST/PUT | `/api/v1/clientes/{id}/contatos` | CRUD de contatos do cliente | admin, gestor |
| POST | `/api/v1/clientes/{id}/responsaveis` | Vincula responsável por equipe | admin, gestor |

## Configuração de Processos
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET/POST/PUT | `/api/v1/tipos-processo` | CRUD de tipo de processo | admin, gestor |
| GET/POST/PUT | `/api/v1/tipos-processo/{id}/campos` | CRUD de campos personalizados | admin, gestor |
| GET/POST/PUT | `/api/v1/tipos-processo/{id}/fluxos` | CRUD de fluxos | admin, gestor |
| PUT | `/api/v1/fluxos/{id}/definir-padrao` | Define fluxo como padrão | admin, gestor |
| GET/POST/PUT | `/api/v1/fluxos/{id}/etapas` | CRUD de etapas (qualquer tipo) | admin, gestor |
| PUT | `/api/v1/etapas/{id}/reordenar` | Altera ordem das etapas | admin, gestor |
| GET/POST/PUT | `/api/v1/etapas/{id}/notificacoes` | CRUD de notificações da etapa | admin, gestor |

## Demandas (execução)
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET | `/api/v1/demandas` | Lista com filtro/ordenação/paginação | autenticado, `processos:read` |
| POST | `/api/v1/demandas` | Abre nova demanda | permissão do tipo de processo, `processos:write` |
| GET | `/api/v1/demandas/{id}` | Detalhe completo (etapas, histórico, campos) | autenticado |
| PATCH | `/api/v1/demandas/{id}/responsavel` | Altera responsável | gestor, admin |
| PATCH | `/api/v1/demandas/{id}/prioridade` | Altera prioridade | gestor, admin, `processos:prioritize` |
| POST | `/api/v1/demandas/{id}/cancelar` | Cancela demanda | gestor, admin |
| POST | `/api/v1/demandas/{id}/reabrir` | Reabre demanda concluída | admin |
| PATCH | `/api/v1/demandas/bulk` | Edição em massa (responsável/prioridade/status) | gestor, admin |
| POST | `/api/v1/execucao-etapas/{id}/concluir` | Conclui etapa (dispara handler do tipo) | conforme `configuracao_acesso` da etapa |
| POST | `/api/v1/execucao-etapas/{id}/comentarios` | Adiciona comentário | responsável, gestor, admin |
| POST | `/api/v1/execucao-etapas/{id}/anexos` | Upload de anexo | responsável, gestor, admin |
| GET | `/api/v1/execucao-etapas/{id}/historico` | Timeline completa da execução | autenticado com acesso à demanda |

## Regras de Automação
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET/POST/PUT | `/api/v1/regras-automacao` | CRUD de regras | admin |
| POST | `/api/v1/regras-automacao/{id}/testar` | Dry-run da regra contra dados atuais | admin |

## Documentos e Portal
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| POST | `/api/v1/documentos` | Upload de documento | autenticado |
| GET | `/api/v1/documentos/{id}/download-url` | Gera pre-signed URL de download | autenticado com acesso |
| POST | `/api/v1/documentos/{id}/aprovar` | Aprova documento | gestor, admin |
| POST | `/api/v1/solicitacoes-documento` | Cria solicitação ao cliente | analista, gestor |
| POST | `/api/v1/portal/auth/login` | Login do cliente no portal | público (esquema separado) |
| GET | `/api/v1/portal/pendencias` | Pendências do cliente autenticado | cliente autenticado |
| POST | `/api/v1/portal/solicitacoes/{id}/enviar` | Cliente envia documento solicitado | cliente autenticado |

## Dashboards, Auditoria e Integrações
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET | `/api/v1/dashboards/equipe/{id}` | Métricas de desempenho da equipe | gestor, admin |
| GET | `/api/v1/central-pendencias` | Contadores por urgência | gestor, admin |
| GET | `/api/v1/logs-atividade` | Log de auditoria filtrável | admin |
| GET/POST | `/api/v1/tokens-api` | Emissão/listagem de tokens de API | admin, gestor |
| DELETE | `/api/v1/tokens-api/{id}` | Revoga token | admin, gestor |
| GET/PUT | `/api/v1/integracoes/{tipo}` | Configura integração externa | admin |
| GET/POST | `/api/v1/webhooks` | CRUD de webhooks de saída | admin |

## Health e observabilidade
| Método | Rota | Descrição |
|---|---|---|
| GET | `/health/live` | Liveness probe |
| GET | `/health/ready` | Readiness probe (checa DB, Redis, RabbitMQ) |
