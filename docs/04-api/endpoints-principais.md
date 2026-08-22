# Catálogo de Endpoints Principais

Convenções gerais em [`convencoes-api.md`](convencoes-api.md). A fonte de verdade em tempo de
execução é sempre o OpenAPI gerado a partir do código, disponível em `/swagger` — este catálogo
é uma referência de navegação rápida, não substitui a especificação interativa.

**Legenda:** ✅ implementado e testado · 🗓️ planejado (desenhado no roadmap, ainda não construído).
Rotas ✅ abaixo são as rotas reais do código; a coluna "Escopo" resume a política de autorização —
para o valor exato de cada endpoint, ver `[Authorize]`/`RequireAuthorization` no Swagger.

## ✅ Identidade e Tenants
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| POST | `/api/v1/tenants` | Cadastro de novo escritório (onboarding) | público |
| POST | `/api/v1/auth/login` | Login, retorna access token + refresh token (cookie httpOnly) | público |
| POST | `/api/v1/auth/refresh` | Renova access token a partir do refresh token | refresh token válido |
| POST | `/api/v1/auth/logout` | Revoga refresh token atual | autenticado |
| GET | `/api/v1/me` | Identidade do usuário autenticado (id, tenant, perfil, nome) | autenticado |

## ✅ Equipes e Clientes
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET/POST | `/api/v1/equipes` | Lista / cria equipe | autenticado / gestor, admin |
| GET/PUT | `/api/v1/equipes/{id}` | Detalhe / atualiza equipe | autenticado / gestor, admin |
| POST/DELETE | `/api/v1/equipes/{id}/membros` `/membros/{usuarioId}` | Adiciona / remove membro com papel | gestor, admin |
| POST | `/api/v1/equipes/{id}/desativar` · `/reativar` | Ativa/desativa equipe | admin |
| GET/POST | `/api/v1/clientes` | Lista (filtro/paginação) / cadastra cliente | autenticado / gestor, admin |
| GET/PUT | `/api/v1/clientes/{id}` | Detalhe / atualiza cliente | autenticado / gestor, admin |
| POST | `/api/v1/clientes/{id}/contatos` | Adiciona contato do cliente | gestor, admin |
| POST | `/api/v1/clientes/{id}/inativar` · `/reativar` · `/suspender` | Transições de status do cliente | gestor, admin |
| POST | `/api/v1/clientes/{id}/responsaveis` | Vincula responsável por equipe | gestor, admin |
| POST | `/api/v1/contatos-cliente/{id}/desativar` · `/reativar` | Transições de status do contato | gestor, admin |
| DELETE | `/api/v1/responsaveis-cliente/{id}` | Remove vínculo de responsável | gestor, admin |
| GET/POST | `/api/v1/grupos-clientes` | Lista / cria grupo econômico de clientes | gestor, admin |

## ✅ Configuração de Processos
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET/POST | `/api/v1/tipos-processo` | Lista / cria tipo de processo | autenticado / gestor, admin |
| GET/PUT | `/api/v1/tipos-processo/{id}` | Detalhe / atualiza tipo de processo | autenticado / gestor, admin |
| POST | `/api/v1/tipos-processo/{id}/ativar` · `/desativar` | Ativa/desativa tipo de processo | gestor, admin |
| PUT | `/api/v1/tipos-processo/{id}/permissoes-inicio` | Define quem pode abrir demanda desse tipo | gestor, admin |
| POST | `/api/v1/tipos-processo/{id}/campos` | Cria campo personalizado | gestor, admin |
| PUT/DELETE | `/api/v1/campos-personalizados/{id}` | Atualiza / remove campo personalizado | gestor, admin |
| POST | `/api/v1/tipos-processo/{id}/fluxos` | Cria fluxo para o tipo de processo | gestor, admin |
| PUT/DELETE | `/api/v1/fluxos/{id}` | Atualiza / remove fluxo | gestor, admin |
| POST | `/api/v1/fluxos/{id}/definir-padrao` | Define fluxo como padrão do tipo de processo | gestor, admin |
| GET/POST | `/api/v1/fluxos/{fluxoId}/etapas` | Lista / cria etapa no fluxo | autenticado / gestor, admin |
| GET/PUT/DELETE | `/api/v1/etapas/{id}` | Detalhe / atualiza / remove etapa | autenticado / gestor, admin |
| PUT | `/api/v1/etapas/{id}/configuracao-acesso` | Define quem pode concluir a etapa | gestor, admin |
| GET | `/api/v1/tipos-etapa` | Lista os tipos de etapa suportados pelo motor (Comum, Condicional, Automatizada, Notificação, Agendamento, Subprocesso, Conclusão, União) | autenticado |

## ✅ Demandas e Execução
| Método | Rota | Descrição | Escopo |
|---|---|---|---|
| GET | `/api/v1/demandas` | Lista com filtro composto (status, prioridade, responsável, cliente, tipo, etapa, intervalo de datas), ordenação cumulativa e paginação | autenticado (Analista só vê a própria fila) |
| POST | `/api/v1/demandas` | Abre nova demanda — dispara o motor de execução (fork/join) a partir do fluxo padrão | conforme permissão de início do tipo de processo |
| GET | `/api/v1/demandas/{id}` | Detalhe completo da demanda | autenticado com acesso |
| GET | `/api/v1/demandas/kanban` | Quadro por etapa (colunas = etapas do fluxo padrão, ordenadas) | autenticado |
| POST | `/api/v1/demandas/{id}/responsavel` | Atribui responsável | gestor, admin |
| PATCH | `/api/v1/demandas/{id}/prioridade` | Altera prioridade | gestor, admin |
| POST | `/api/v1/demandas/{id}/cancelar` | Cancela demanda | gestor, admin |
| PATCH | `/api/v1/demandas/bulk` | Edição em massa — melhor-esforço, retorna sucesso/falha por item (responsável, prioridade ou cancelamento) | gestor, admin |
| POST | `/api/v1/execucoes-etapa/{id}/concluir` | Conclui a execução da etapa atual — o orquestrador decide o próximo passo (avançar, ramificar fork, aguardar join) | conforme configuração de acesso da etapa |
| GET/POST | `/api/v1/execucoes-etapa/{id}/comentarios` | Lista / adiciona comentário na execução | autenticado com acesso |
| GET/POST | `/api/v1/execucoes-etapa/{id}/anexos` | Lista / envia anexo (armazenado em object storage) | autenticado com acesso |
| GET | `/api/v1/anexos/{id}/conteudo` | Baixa o binário do anexo | autenticado com acesso |
| GET | `/api/v1/execucoes-etapa/{id}/historico` | Timeline completa da execução da etapa | autenticado com acesso |

## 🗓️ Regras de Automação (planejado)
| Método | Rota | Descrição |
|---|---|---|
| GET/POST/PUT | `/api/v1/regras-automacao` | CRUD de regras declarativas ("se X, então Y") |
| POST | `/api/v1/regras-automacao/{id}/testar` | Dry-run da regra contra dados atuais |

## 🗓️ Documentos e Portal do Cliente (planejado)
| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/v1/documentos` | Upload de documento avulso |
| GET | `/api/v1/documentos/{id}/download-url` | Gera pre-signed URL de download |
| POST | `/api/v1/documentos/{id}/aprovar` | Aprova documento |
| POST | `/api/v1/solicitacoes-documento` | Cria solicitação de documento ao cliente |
| POST | `/api/v1/portal/auth/login` | Login do cliente no portal (esquema de auth separado, ver ADR-005) |
| GET | `/api/v1/portal/pendencias` | Pendências do cliente autenticado |
| POST | `/api/v1/portal/solicitacoes/{id}/enviar` | Cliente envia o documento solicitado |

> Anexos de execução (`/api/v1/execucoes-etapa/{id}/anexos`) já existem hoje — o que falta aqui é
> o módulo de documentos avulsos + o portal como superfície separada para o cliente final.

## 🗓️ Dashboards, Auditoria e Integrações (planejado)
| Método | Rota | Descrição |
|---|---|---|
| GET | `/api/v1/dashboards/equipe/{id}` | Métricas de desempenho da equipe |
| GET | `/api/v1/central-pendencias` | Contadores por urgência |
| GET | `/api/v1/logs-atividade` | Log de auditoria filtrável |
| GET/POST/DELETE | `/api/v1/tokens-api` | Emissão/listagem/revogação de tokens de API para integrações |
| GET/PUT | `/api/v1/integracoes/{tipo}` | Configura integração externa |
| GET/POST | `/api/v1/webhooks` | CRUD de webhooks de saída |

## ✅ Real-time e observabilidade
| Método | Rota | Descrição |
|---|---|---|
| WS | `/hubs/kanban` | SignalR — grupo por equipe, evento `quadroAlterado` (ver [convenções §7](convencoes-api.md#7-real-time-signalr)) |
| GET | `/health/live` | Liveness probe |
