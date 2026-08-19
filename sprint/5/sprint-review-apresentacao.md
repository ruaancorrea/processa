# Sprint 5 — Apresentação

Sprint 5 do cronograma (`docs/07-roadmap/backlog-sprints.md`) — Execução de Demandas · Board PROJ (PROJ-20, subtasks PROJ-52 a PROJ-56) · Branch `feature/processa-sprint5-execucao-demandas`

## 1. Entregas

| Feature | Status | Stack |
|---|---|---|
| Entidade `Demanda` + `ExecucaoEtapa` + orquestrador real dos 8 handlers do Sprint 4 | Pronto | Domain, Application (`OrquestradorExecucao`), EF Core/Postgres |
| Abertura de demanda por formulário e por API (mesmo endpoint) | Pronto | `CriarDemandaCommand` |
| Abertura automática por Etapa de Subprocesso | Pronto | `CriadorSubprocesso` (real, substitui o stub do Sprint 4) |
| Atribuição de responsável nos 3 modos (fixo/dinâmico/manual) | Pronto | `CriarDemandaCommandHandler.ResolverResponsavelAsync` |
| Progressão linear, Condicional (redirecionamento) e **fork/join real** (paralelismo genuíno) | Pronto | `OrquestradorExecucao`, `EtapaCondicionalHandler` redesenhado |
| Comentários e anexos por execução de etapa | Pronto | `ComentarioExecucao`, `AnexoExecucao` |
| Anexos com storage real (não stub) | Pronto | MinIO via `AWSSDK.S3`, `ArmazenamentoArquivoS3` |
| Histórico auditável de eventos por execução | Pronto | `HistoricoExecucaoEtapa` |
| `PermissoesInicio` (Sprint 3) finalmente enforçada | Pronto | `CriarDemandaCommandHandler.PodeIniciar` |
| Mitigação de SSRF na Etapa Automatizada (Sprint 4) | Pronto | `ClienteHttpEtapaConnectGuard` |

## 2. Decisões de arquitetura

**Fork/join real, não simulado — decisão explícita do usuário.** A alternativa mais simples (União apenas espera pela conclusão de etapas nomeadas, sem paralelismo de verdade) foi descartada em favor do desenho correto: uma Demanda pode ter múltiplas `ExecucaoEtapa` simultaneamente ativas quando uma Condicional casa mais de um ramo ao mesmo tempo. Isso exigiu evoluir um contrato já mergeado no Sprint 4 (`ResultadoExecucaoEtapa.ProximaEtapaId` → `ProximasEtapasIds`, lista) — mudança deliberada, não acidental.

**Anexos usam MinIO real via `AWSSDK.S3`, não um stub — segunda decisão explícita do usuário.** O serviço `minio` já existia no `docker-compose.yml` desde o Sprint 0, nunca ligado. SDK oficial AWS (não o dedicado do MinIO) por portabilidade — funciona com qualquer endpoint S3-compatível via `ServiceURL` customizado.

**`ArmazenamentoArquivoS3` registrado como Singleton, não Scoped** (achado em revisão, corrigido antes do PR) — `AmazonS3Client` é thread-safe e caro de construir (recomendação da própria AWS); Scoped recriava o cliente a cada requisição e impedia qualquer cache de "bucket já confirmado" funcionar.

**Duas pendências de sprints anteriores, explicitamente marcadas "resolver quando o Sprint 5 ligar a execução real", foram resolvidas aqui** (não são escopo novo, são débito técnico já rastreado): `PermissoesInicio` (configurável desde o Sprint 3, nunca checada) e a mitigação de SSRF da Etapa Automatizada (Sprint 4). Ver `.faf/pendencias.faf`.

## 3. Desvios do planejado

Nenhum desvio de escopo formal. O que mudou de estimativa: as duas pendências herdadas (seção 2) não estavam no "pronto quando" original do Sprint 5, mas o próprio texto delas já apontava este sprint como o gatilho certo — tratadas como parte do fechamento, não como scope creep.

## 4. Bugs e resoluções

**Achados via TDD no orquestrador (lógica single-threaded, antes de qualquer exposição HTTP) — 3 bugs reais de design:**
1. Gatilho errado pra propagação de União: código original só reavaliava uniões quando uma etapa **Condicional** concluía — mas a Condicional decide o fork quase instantaneamente; quem representa "esse ramo terminou de verdade" é a etapa no fim de cada branch. Fix: propagação roda pra qualquer etapa que conclui, sem filtro de tipo.
2. Ramos de fork caindo de volta pra travessia linear por `Ordem` ao concluir, indo pro ramo **irmão** em vez de esperar a União. Fix: `AlgumaUniaoAguardaAsync` detecta ponta-de-fork e suprime o fallback linear.
3. `ExecucaoEtapa` da União criada antecipadamente (antes de qualquer ramo navegar até ela) nascia `Pendente` e nunca era reavaliada (o gatilho original só disparava para `Aguardando`). Fix: reavaliação incondicional — `AvancarRamoAsync` já tinha guarda própria isentando União do "já processada, pula".

**Achado em QA manual contra MinIO real (não em unit test):** primeiro upload de anexo de toda a vida da instância MinIO devolvia 500 — `AWSSDK.S3` v4 contra MinIO retorna `ListBucketsAsync().Buckets` como `null` (não lista vazia) quando a conta ainda não tem bucket nenhum, mesma classe de bug do jsonb/Postgres do Sprint 4 (só aparece contra o serviço real). Fix: checagem null-safe.

**Achado na revisão `find-bugs` (a única das 5 skills do checklist com processo real, não boilerplate genérico — mesmo achado dos Sprints 2-4):** corrida real quando dois ramos diferentes de um fork são, cada um, o primeiro a alcançar a mesma União nunca antes visitada — o perdedor da corrida perde o registro do seu `DesdobramentoAguardado` junto com a falha de índice único. **Decisão consciente de não corrigir agora** (janela estreita, correção de concorrência sem teste de carga dedicado é mais risco que benefício) — documentado com plano de mitigação em `.faf/pendencias.faf`. Duas outras correções de menor risco saíram da mesma revisão e foram aplicadas: limite explícito de corpo de requisição (25 MB, alinhado à regra de negócio de anexo) e cache de "bucket confirmado" (eliminando uma chamada de rede redundante por upload).

## 5. Código relevante

- `backend/src/Processa.Modules.Processos/Application/Demandas/OrquestradorExecucao.cs` — o motor de verdade; vale ler `PropagarParaUnioesQueAguardamAsync` e `AvancarRamoAsync` junto com os comentários que documentam as 3 corridas/bugs já corrigidos e a que ficou pendente.
- `backend/src/Processa.Modules.Processos/Infrastructure/ClienteHttpEtapa.cs` — `ClienteHttpEtapaConnectGuard.ConectarAsync`, a mitigação de SSRF via `SocketsHttpHandler.ConnectCallback`.
- `backend/tests/Processa.UnitTests/Modules/Processos/Application/OrquestradorExecucaoTests.cs` — o teste do fork completo (2 ramos + União), com fakes de repositório escritos à mão (NSubstitute sozinho não sustenta a semântica stateful de Add-depois-Get).

## 6. Bloqueios e dependências

Nenhum bloqueio pro fechamento deste sprint. Um risco técnico documentado conscientemente (corrida rara no fork/join, seção 4) — ver `.faf/pendencias.faf`.
