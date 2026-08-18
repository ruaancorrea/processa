# Sprint 4 — Resumo Geral

Sprint 4 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint4-motor-processos-etapas`

## Por Epic/Feature

| Feature | Status (Jira) | Bloqueios |
|---|---|---|
| PROJ-45 — Etapa Comum e Etapa de Conclusão | Feito | Nenhum |
| PROJ-46 — Etapa Condicional (desdobramento) com redirecionamento de fluxo | Feito | Nenhum |
| PROJ-47 — Etapa Automatizada (chamada HTTP + validação de resposta) | Feito | Nenhum — única já executável de verdade |
| PROJ-48 — Etapa de Notificação e Etapa de Agendamento | Feito | Envio real de notificação é Sprint 8 |
| PROJ-49 — Etapa de Subprocesso com herança de campos personalizados do pai | Feito (configuração/decisão) | Herança real de campos exige Demanda persistida — Sprint 5 |
| PROJ-50 — Etapa de União (fork/join) aguardando múltiplos desdobramentos | Feito (configuração/decisão) | Verificação real de desdobramentos exige tabela do Sprint 5 |
| PROJ-51 — Configuração de acesso por etapa (quem pode alterar/concluir) | Feito | Nenhum |

## Por Tecnologia

**Novas dependências:** nenhuma — reaproveita `Microsoft.Extensions.Http` (já parte do SDK ASP.NET Core, `AddHttpClient` é built-in) pro `ClienteHttpEtapa`.

**Mudanças relevantes de dependência:** nenhuma.

**Mudanças em contratos de API:**
- `POST/GET /api/v1/fluxos/{fluxoId}/etapas` — novos
- `GET/PUT/DELETE /api/v1/etapas/{id}` — novos
- `PUT /api/v1/etapas/{id}/configuracao-acesso` — novo

**Schema de banco (EF Core migrations):** 1 migration nova — `AdicionarEtapas` (tabela `etapas`: colunas `configuracao` **json** — não jsonb, ver seção de bugs — e `configuracao_acesso` jsonb, índice `(TenantId, FluxoId)`).

## Métricas

| Métrica | Valor |
|---|---|
| Arquivos alterados | 44 |
| Linhas | +2.418 / -3 (working tree; sprint ainda não commitado no momento da medição) |
| Testes automatizados (backend) | 272 (fim do Sprint 3: 24 arquitetura + 196 unitários + 52 integração) → **348** (24 arquitetura + 259 unitários + 65 integração) |
| Cobertura de testes (backend) | 83,3% (Sprint 3) → **83,46%** linha (gate: 80%) |
| Vulnerabilidades (`dotnet list package --vulnerable`) | 0 |
| Snyk (`snyk test --all-projects` + `snyk code test`) | SCA: 0 vulneráveis (11 projetos, 246 dependências). SAST: 4 abertos, mesmos falsos-positivos já documentados desde o Sprint 2 — nenhum achado novo |
| QA manual (curl + inspeção direta do Postgres via psql) | Fluxo completo criando os 8 tipos de etapa, incluindo o bug real do jsonb/discriminador pego e corrigido durante a verificação manual — ver `qa/processa-api-sprint4.http` |
| Rodadas de revisão | 1 (revisão manual do diff completo — skills nomeadas no checklist continuam devolvendo boilerplate genérico de Sentry/Django, mesmo achado do Sprint 3) |
| Migrations novas | 1 (`AdicionarEtapas`, regenerada uma vez após o achado do jsonb pra manter o histórico limpo — sem commit intermediário da versão errada) |
| Bugs reais encontrados testando manualmente | 1 (jsonb não preserva ordem de chave — só apareceu inspecionando o dado bruto no Postgres, o teste automatizado sozinho só acusava um sintoma genérico) |
| Bugs reais encontrados em revisão manual | 1 (FluentValidation sem espelho da validação interna de Configuracao — corrigido reaproveitando `ConfiguracaoEtapa.Validar()`) |

## Risco técnico

- **Etapa Automatizada é um vetor de SSRF em potencial quando a execução real ligar (Sprint 5).** Hoje `ConfiguracaoEtapaAutomatizada.Url` só valida formato de URI; nada impede configurar um endpoint interno/metadata de nuvem. Inofensivo agora porque nada no código shipado ainda invoca o handler contra dado real — mas precisa de mitigação (bloqueio de IP privado/link-local, timeout, limite de resposta) antes do Sprint 5 ligar a execução de verdade. Ver `.faf/pendencias.faf`.
- **Referências etapa-a-etapa (Condicional/União) não são validadas contra etapas que existem de verdade.** `Guid`s de destino só são checados como não-vazios, não como referências reais. Decisão consciente de adiar — parece mais natural como validação de "publicar/ativar o fluxo" (possivelmente Sprint 5) do que checagem por-etapa isolada.
- **Coluna `Configuracao` é `json`, não `jsonb`** — única exceção no projeto. Documentado com justificativa técnica clara (`.faf/decisions.faf`) pra não ser "corrigida" de volta pra jsonb por engano numa limpeza futura.

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| Mitigação de SSRF na Etapa Automatizada | Handler ainda não é invocado contra dado real | Resolver antes do Sprint 5 ligar a execução real |
| Validação de referências etapa-a-etapa | Exigiria N idas ao banco por ramo/etapa referenciada | Reavaliar ao decidir como fluxos são publicados/ativados |
| `AtualizarCampoPersonalizadoCommand`/`AtualizarEtapaCommand` sem regra "tipo aceita/exige configuração" espelhada em FluentValidation | `Tipo` não está no corpo do update (imutável), validators são síncronos | Reavaliar só se validators assíncronos virarem padrão aceito no projeto |
| PermissoesInicio (Sprint 3) e ConfiguracaoAcesso (Sprint 4) ainda sem enforcement real | Endpoint de Demanda não existe | Implementar no handler de criação/conclusão de Demanda, Sprint 5 |
