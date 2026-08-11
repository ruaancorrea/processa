# Processa — Requisitos Funcionais

**Versão:** 1.0 · **Status:** Especificação para o MVP · Ver [visão do produto](../00-visao-geral/visao-do-produto.md) para contexto e motivação.

---

## 1. Perfis de acesso

### 1.1 Administrador
- Acesso irrestrito ao tenant (escritório): usuários, equipes, integrações, tokens de API, configuração de qualquer processo.
- Visualiza dados e dashboards de todas as equipes do escritório.

### 1.2 Gestor
- Visão completa dos membros e demandas da própria equipe.
- Dashboards de desempenho da equipe; delega processos, atribui responsáveis manualmente.
- Configura processos vinculados à própria equipe.

### 1.3 Analista
- Visualiza e executa as demandas atribuídas a si.
- Pode iniciar processos conforme permissão definida pelo gestor/admin.
- Acesso limitado às telas de configuração.

### 1.4 Cliente (portal externo)
- Acesso restrito ao próprio painel de pendências, documentos e solicitações.
- Não enxerga nada da operação interna do escritório — apenas o que foi explicitamente exposto a ele.

---

## 2. Módulo: Tenants e Identidade

Cada escritório contábil é um **tenant** isolado — dados, usuários e configurações não são visíveis entre tenants.

**Regras:**
- Um tenant é criado no onboarding (nome do escritório, CNPJ, domínio opcional para o portal do cliente).
- Um usuário pertence a um único tenant. Um cliente (pessoa jurídica atendida pelo escritório) pertence a um único tenant.
- Autenticação via JWT (access token 15 min, refresh token 7 dias). RBAC validado em toda rota da API.
- Bloqueio de conta após 5 tentativas de login falhas consecutivas, por 15 minutos.

---

## 3. Módulo: Equipes e Usuários

**Usuário** — id, nome, e-mail (único no tenant), senha (hash bcrypt), perfil (`admin`, `gestor`, `analista`), nível de experiência (inteiro), status ativo/inativo.

**Equipe** — id, nome, descrição. Um usuário pode pertencer a múltiplas equipes, com papéis diferentes em cada uma. Um tipo de processo pertence a exatamente uma equipe.

---

## 4. Módulo: Clientes

**Grupo de clientes** — agrupamento lógico para histórico compartilhado e notificações coletivas.

**Cliente** — razão social, CNPJ (único no tenant), código externo (integração com sistema contábil legado), grupo (opcional), regime tributário, data de entrada, status (`ativo`, `inativo`, `suspenso`).

**Contato do cliente** — nome, e-mail, telefone, celular, ativo. Um cliente pode ter múltiplos contatos, selecionáveis individualmente ou em bloco nas notificações.

**Vínculo responsável-cliente por equipe** — um ou mais membros de uma equipe podem ser marcados como responsáveis por um cliente específico naquela equipe (independente do responsável de uma demanda pontual). Usado como destinatário de notificação (`responsáveis do cliente na equipe X`) e para calcular recorrência de atendimento na atribuição manual.

---

## 5. Módulo: Configuração de Processos

Um **tipo de processo** é o template que define como uma categoria de trabalho deve ser executada — não é uma execução em si.

**Campos:** nome, descrição, equipe vinculada, responsável obrigatório (booleano), modo de atribuição (`fixo` | `dinâmico` | `manual`), responsável fixo (se modo fixo), permissões de início (quais perfis/usuários podem abrir uma demanda deste tipo), ativo/inativo.

**Campos personalizados** — zero ou mais por tipo de processo: nome, tipo (`texto`, `número`, `data`, `lista`, `booleano`, `monetário`, `arquivo`, `usuário`), opções (para lista), obrigatório, ordem de exibição.

**Fluxo** — um tipo de processo tem um ou mais fluxos (agrupamento linear de etapas). Exatamente um fluxo é o padrão por tipo de processo; o serviço garante essa unicidade transacionalmente ao criar/alterar fluxos.

---

## 6. Módulo: Motor de Processos — Tipos de Etapa

Um fluxo tem uma ou mais etapas, executadas em ordem. Cada etapa tem: nome, descrição, tipo, ordem, configuração (JSON específica por tipo), configuração de acesso (`{"pode_alterar": ["responsavel","gestor","admin","sistema"]}`).

| Tipo | Comportamento |
|---|---|
| **Comum** | Etapa manual — o responsável marca como concluída ou não concluída |
| **Condicional** | Valida um campo (status, % conclusão, ou campo personalizado) e redireciona o fluxo: próxima etapa, volta para etapa anterior, ou vai para etapa de outro fluxo do mesmo processo |
| **Automatizada** | Chamada HTTP (GET/POST) a uma API externa; valida resposta por status code ou por campo do JSON retornado; sucesso avança, falha trava a etapa |
| **Notificação** | Dispara notificação por canal configurado — ver [Módulo 8](#8-módulo-notificações) |
| **Agendamento** | Marca um evento (reunião, prazo), opcionalmente sincronizado com Google Calendar; a data/hora definida é usada como `data_fim_real` da execução |
| **Subprocesso** | Inicia outro tipo de processo; a etapa pai aguarda a conclusão do subprocesso; campos personalizados do pai ficam disponíveis (leitura/escrita) no subprocesso |
| **Conclusão** | Marcador de encerramento — ao atingir esta etapa, o processo é marcado `concluído`, `data_fim_real` registrada, notificações de conclusão disparadas |
| **União** | Fork/join — aguarda a conclusão de todos os sub-fluxos paralelos disparados por uma ou mais etapas Condicionais anteriores; avança só quando todos concluírem |

---

## 7. Módulo: Kanban e Painel Operacional

### 7.1 Kanban
- Colunas configuráveis por tipo de processo (padrão: A Fazer, Em Andamento, Revisão, Concluído), mapeadas ao status da demanda/etapa.
- Drag-and-drop entre colunas, respeitando `configuracao_acesso` da etapa atual.
- Atualização em tempo real (SignalR) — outro usuário movendo um card reflete instantaneamente sem reload.

### 7.2 Visão em lista
- **Gestor:** vê toda a fila da equipe. Topo: demandas sem responsável. Abaixo: pendentes e em andamento.
- **Analista:** vê apenas a própria fila.
- **Filtros:** status, responsável, prioridade, data de início/fim prevista, tempo decorrido, cliente (CNPJ ou grupo), tipo de processo, etapa atual, campos personalizados relevantes.
- **Ordenação:** por qualquer coluna, inclusive cumulativa (ex: prioridade DESC, data_início ASC).
- **Seleção e edição em massa:** alterar responsável, prioridade, status; cancelar demandas selecionadas.

---

## 8. Módulo: Notificações

Toda etapa pode ter uma ou mais notificações configuradas, cada uma com destinatário, canal e momento independentes.

**Destinatários:** `responsável`, `contato_cliente` (um ou todos), `usuário` (id), `equipe` (id), `responsável_cliente_equipe` (todos os vinculados ao cliente naquela equipe).

**Canais:** interno (tempo real + central de notificações), e-mail (HTML rich text via editor com variáveis), WhatsApp (provedor externo).

**Momentos de disparo:** `ao_entrar`, `ao_sair`, `lembrete_periódico` (a cada N dias/semanas/meses/anos enquanto na etapa), `lembrete_fixo_pós_abertura` (X dias após abertura do processo), `lembrete_antes_data_alvo` (Y dias antes de `data_fim_prevista`).

**Variáveis de template:** `{{nome_processo}}`, `{{cliente}}`, `{{responsavel}}`, `{{etapa_atual}}`, `{{data_inicio}}`, `{{data_fim_prevista}}`, `{{prioridade}}`, `{{campo_NomeDoCampo}}`.

---

## 9. Módulo: Motor de Regras e Automações

Regras declaráveis sem código, avaliadas de forma assíncrona sobre eventos do domínio (mudança de status, prazo se aproximando, etapa concluída).

**Estrutura de uma regra:** `{ trigger, condição, ação }`.

**Exemplos de regras cobertas no MVP:**
- Documento não recebido X dias antes do vencimento → notificação de alerta + escalonamento ao gestor.
- Tarefa em uma etapa há mais de X dias → escalona (notifica gestor, eleva prioridade).
- Etapa concluída → libera automaticamente a próxima (quando não há dependência manual).
- Threshold de SLA atingido (ex: 80% do prazo consumido) → notificação de alerta.

Regras ficam associadas a um tipo de processo ou globalmente ao tenant; o motor as avalia via worker assíncrono (RabbitMQ), não em linha com a requisição HTTP.

---

## 10. Módulo: Início e Execução de Demandas

Uma **demanda** é a instância de execução de um tipo de processo — vinculada a um cliente, com responsável, etapas percorridas e histórico completo.

### 10.1 Formas de início
- **Formulário:** usuários com permissão configurada; campos padrão (tipo de processo, cliente, obrigatórios; responsável, prioridade, data_fim_prevista, opcionais) + campos personalizados.
- **API:** endpoint autenticado por token, escopo `processos:write`.
- **Subprocesso:** automático, disparado por uma etapa de Subprocesso; herda cliente (e opcionalmente responsável) do processo pai.

### 10.2 Atribuição de responsável
- **Fixo:** definido na configuração do tipo de processo, atribuído automaticamente.
- **Dinâmico:** sistema atribui ao membro da equipe com menor número de demandas ativas.
- **Manual:** se `responsavel_obrigatorio=true`, quem abre escolhe no cadastro; se `false`, demanda nasce `sem_responsável` e o gestor recebe notificação e atribui depois. A listagem de membros para atribuição mostra nome, nº de demandas ativas, nível de experiência e recorrência de atendimento ao cliente/grupo.

### 10.3 Progressão e histórico
- Etapas executam em ordem linear; só etapas Condicionais alteram a progressão.
- Cada execução de etapa registra `iniciado_em`/`concluído_em` (tempo real de execução calculável pela diferença).
- `historico_execucao_etapa` registra: mudança de responsável, mudança de status, comentário adicionado, anexo adicionado, execução de API, reabertura de etapa — timeline completa visível na interface.
- Comentários e anexos: usuários autorizados (responsável, gestor da equipe, admin) podem adicionar enquanto a execução estiver `em_andamento`, `pendente` ou `aguardando`; somente leitura após conclusão (salvo reabertura).

---

## 11. Módulo: Documentos e Portal do Cliente

### 11.1 Documentos (lado escritório)
- Upload com nome único gerado (UUID) preservando nome original para exibição; versionamento (histórico de versões do mesmo documento lógico); categorias; fluxo de aprovação opcional (pendente → aprovado/rejeitado).
- Solicitação de documento a um cliente: cria uma pendência visível no portal, com prazo.

### 11.2 Portal do cliente
- Login próprio (não compartilha credenciais com o sistema interno), por convite/contato cadastrado.
- Tela inicial: resumo de pendências (documentos, solicitações, tarefas aguardando o cliente).
- Upload de documento solicitado, com validação de tipo/tamanho.
- Histórico de documentos enviados e status de cada um.

---

## 12. Módulo: Dashboards, Central de Pendências e Auditoria

- **Dashboard de equipe:** tempo médio de execução por etapa, volume de processos por tipo, taxa de cumprimento de SLA.
- **Central de Pendências:** contadores por urgência — atrasadas, vence hoje, vence esta semana, em dia — com drill-down até a demanda específica.
- **Log de auditoria:** toda ação sobre entidade crítica (create/update/delete/status_change) registrada com usuário (nullable para ações automáticas), dados antes/depois (JSON snapshot), IP, timestamp. Retenção mínima de 90 dias. Consulta via API e interface, filtrável por entidade, usuário, período, ação.

---

## 13. Módulo: Integrações e API Pública

- **Tokens de API:** nome descritivo, escopos (`processos:read`, `processos:write`, `processos:prioritize`, `clientes:read`, `clientes:write`, `config:read`, `webhooks:manage`), expiração, status ativo. Token exibido uma única vez na geração; armazenado como hash SHA-256.
- **Rota de priorização via API:** `PATCH /api/v1/processos/{id}/prioridade`, escopo dedicado — permite que sistemas externos (ex: alertas fiscais) elevem prioridade automaticamente.
- **Webhooks de saída:** eventos de processo (criado, concluído, atrasado) notificados a URLs configuradas pelo tenant.
- **Integrações de terceiros:** Gmail (OAuth2, envio de e-mail), Google Calendar (OAuth2, eventos de agendamento), WhatsApp (provedor externo, ex: gateway de Business API). Credenciais armazenadas cifradas (AES-256-GCM) no banco.

---

## 14. Requisitos não-funcionais (resumo)

Ver detalhamento em [`../02-arquitetura/visao-arquitetural.md`](../02-arquitetura/visao-arquitetural.md) e [`../05-seguranca/politica-de-seguranca.md`](../05-seguranca/politica-de-seguranca.md).

| Categoria | Requisito |
|---|---|
| Performance | Paginação obrigatória em listagens (padrão 20, máx 100); p95 < 300ms em endpoints de leitura simples |
| Escalabilidade | API stateless; workers de mensageria escaláveis independentemente da API |
| Segurança | JWT + RBAC em toda rota; secrets fora do código; TLS obrigatório em produção |
| Observabilidade | Tracing distribuído (OpenTelemetry) cobrindo API → mensageria → workers |
| Multi-tenancy | Isolamento de dados por tenant garantido em nível de query (filtro obrigatório por `tenant_id`) |

Ver catálogo completo de casos de uso em [`casos-de-uso.md`](casos-de-uso.md).
