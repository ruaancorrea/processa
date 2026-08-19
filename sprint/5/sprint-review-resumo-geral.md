# Sprint 5 — Resumo Geral

Sprint 5 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint5-execucao-demandas`

## Por Epic/Feature

| Feature | Status (Jira) | Bloqueios |
|---|---|---|
| PROJ-52 — Abertura de demanda por formulário | Feito | Nenhum |
| PROJ-53 — Abertura de demanda por API e por subprocesso automático | Feito | Nenhum |
| PROJ-54 — Atribuição de responsável nos 3 modos (fixo/dinâmico/manual) | Feito | Nenhum |
| PROJ-55 — Progressão de etapas com histórico e temporização | Feito | Nenhum |
| PROJ-56 — Comentários e anexos por execução de etapa | Feito | Nenhum |

## Por Tecnologia

**Novas dependências:** `AWSSDK.S3` (v4.0.102.2) — storage real de anexos via MinIO (S3-compatível).

**Mudanças relevantes de dependência:** nenhuma.

**Mudanças em contratos de API:**
- `POST/GET /api/v1/demandas`, `GET /api/v1/demandas/{id}` — novos
- `POST /api/v1/demandas/{id}/responsavel`, `PATCH .../prioridade`, `POST .../cancelar` — novos
- `POST /api/v1/execucoes-etapa/{id}/concluir`, `GET .../historico` — novos
- `POST/GET /api/v1/execucoes-etapa/{id}/comentarios` — novos
- `POST/GET /api/v1/execucoes-etapa/{id}/anexos`, `GET /api/v1/anexos/{id}/conteudo` — novos (upload real via `IFormFile`, download via `Results.File`)
- **Contrato interno alterado** (não HTTP, mas cross-module): `IEtapaHandler.ResultadoExecucaoEtapa.ProximaEtapaId` (Sprint 4) → `ProximasEtapasIds` (lista) + `DadosResultantes` novo — evolução deliberada pra fork real.

**Schema de banco (EF Core migrations):** 1 migration nova — `AdicionarDemandas` (6 tabelas: `demandas`, `execucao_etapas`, `desdobramentos_aguardados`, `historico_execucao_etapa`, `comentarios_execucao`, `anexos_execucao`).

## Métricas

| Métrica | Valor |
|---|---|
| Arquivos alterados | 89 |
| Linhas | +5.163 / -82 |
| Testes automatizados (backend) | 348 (fim do Sprint 4: 24 arquitetura + 259 unitários + 65 integração) → **473** (24 arquitetura + 373 unitários + 76 integração) |
| Cobertura de testes (backend) | 83,46% (Sprint 4) → **83,71%** linha (gate: 80%) |
| Vulnerabilidades (`dotnet list package --vulnerable`) | 0 |
| Snyk (`snyk test --all-projects` + `snyk code test`) | SCA: 0 vulneráveis (11 projetos, inclui `AWSSDK.S3` novo). SAST: 5 abertos — 4 já documentados desde Sprint 2 (segredo dummy em fixture/teste de integração, nunca produção) + 1 falso-positivo novo (taint-flow de NSubstitute mal-interpretado como logging de PII num teste) — nenhum achado real novo |
| QA manual (curl + Postgres real + **MinIO real**) | Fluxo completo: abertura → execução → comentário → anexo (round-trip binário idêntico via MinIO real) → conclusão → prioridade → cancelamento. 1 bug real achado e corrigido durante a verificação (`ListBucketsAsync().Buckets` null) — ver `qa/processa-api-sprint5.http` |
| Rodadas de revisão | 1 (manual + `find-bugs`, que continua sendo a única das 5 skills do checklist com processo real — as outras 4 seguem devolvendo boilerplate genérico de Node/Python, mesmo achado desde o Sprint 2) |
| Migrations novas | 1 (`AdicionarDemandas`, aplicada limpa — sem warning de `ValueComparer` faltando, confirma que o `DadosExecucao` de `ExecucaoEtapa` foi mapeado certo desde o início) |
| Bugs reais encontrados via TDD (orquestrador) | 3 — todos de lógica single-threaded, achados e corrigidos ANTES de qualquer exposição HTTP (ver apresentação, seção 4) |
| Bugs reais encontrados em QA manual | 1 (`AWSSDK.S3` v4 + MinIO, `ListBucketsAsync().Buckets` null na conta vazia) |
| Bugs/achados reais em revisão (`find-bugs`) | 3 — 1 documentado e conscientemente não corrigido (corrida no fork/join), 2 corrigidos (limite de corpo de requisição, cache de bucket) |
| Pendências de sprints anteriores resolvidas | 2 — `PermissoesInicio` nunca enforçada (Sprint 3) e SSRF na Etapa Automatizada (Sprint 4), ambas explicitamente marcadas "resolver quando o Sprint 5 ligar a execução real" |

## Risco técnico

- **Corrida real no primeiro-a-chegar numa União nunca antes visitada.** Dois ramos diferentes de um mesmo fork concluindo quase simultaneamente, sendo cada um o primeiro a alcançar a mesma União, podem fazer o "perdedor" da corrida perder o registro do seu `DesdobramentoAguardado`. Falha de forma visível (422 pro usuário, não um 500 silencioso) mas pode deixar a União travada esperando um desdobramento que nunca chega. Decisão consciente de não corrigir sem teste de carga concorrente dedicado — ver `.faf/pendencias.faf` para o plano de mitigação recomendado (`pg_advisory_xact_lock` ou reordenar gravação).
- **`ClienteHttpEtapaConnectGuard` (mitigação de SSRF) não cobre IPv6 unique-local (fc00::/7).** Cobre loopback, link-local IPv6 e os 3 blocos privados IPv4 (RFC 1918, inclusive mapeados em IPv6) — risco residual aceito pro MVP, já que o vetor de ataque realista (metadata de nuvem, rede interna) é sempre IPv4 hoje.
- **RBAC de execução de Demanda sem isolamento por equipe** (mesma linha já aceita para Clientes/TipoProcesso desde o Sprint 2) — qualquer Gestor/Admin do tenant pode comentar/anexar/concluir/cancelar/baixar anexo de qualquer execução, não só das equipes que gerencia.

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| Corrida no fork/join (primeiro-a-chegar numa União nova) | Correção de concorrência sem teste de carga dedicado é mais risco que benefício agora | `pg_advisory_xact_lock` por União, ou separar a gravação do desdobramento da criação da execução em SaveChanges distintos |
| `ClienteHttpEtapaConnectGuard` sem cobertura de IPv6 unique-local | .NET não tem `IsIPv6UniqueLocal` pronto; vetor de ataque realista é sempre IPv4 hoje | Reavaliar se o piloto rodar em ambiente com IPv6 interno de verdade |
| RBAC sem isolamento por equipe (Clientes/TipoProcesso desde Sprint 2, agora também Demandas) | Mesma decisão consciente já aceita, estendida por consistência | Reavaliar junto, nos dois casos, se o piloto trouxer requisito explícito |
| Restringir configuração de Etapa Automatizada só a Admin (não Gestor) | Decisão de produto maior, não uma mitigação de segurança per se — a mitigação real (SSRF) já foi feita | Reavaliar como decisão de produto separada, se necessário |
