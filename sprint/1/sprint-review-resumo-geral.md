# Sprint 1 — Resumo Geral

Sprint 1 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint1-identidade` · PR #3 + hotfix PR #4

## Por Epic/Feature

| Feature | Status | Bloqueios |
|---|---|---|
| PROJ-33 — Cadastro de tenant (escritório) e onboarding inicial | Pronto — critérios batidos | Nenhum |
| PROJ-34 — Autenticação JWT (access token + refresh token) | Pronto | Nenhum |
| PROJ-35 — RBAC por papel (admin, gestor, analista) em todas as rotas da API | Pronto | Nenhum |
| PROJ-36 — Bloqueio de conta após tentativas de login falhas consecutivas | Pronto | Nenhum |

## Por Tecnologia

**Novas dependências:** `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `BCrypt.Net-Next`, `FluentValidation` (+ `.DependencyInjectionExtensions`), `Testcontainers.PostgreSql`, `NSubstitute`.

**Mudanças relevantes de dependência:** `SSH.NET` (transitiva via `Testcontainers.PostgreSql`) forçada pra `2026.0.0` via referência direta — corrige `GHSA-q939-rpr3-3284` (severidade alta, publicada no mesmo dia do merge), já que a versão mais recente do Testcontainers no momento (4.13.0) ainda trazia `SSH.NET 2025.1.0`, dentro da faixa vulnerável.

**Mudanças em contratos de API:**
- `POST /api/v1/tenants` — novo (onboarding, público)
- `POST /api/v1/auth/login` — novo (seta cookie httpOnly de refresh)
- `POST /api/v1/auth/refresh` — novo (rotaciona o refresh token)
- `POST /api/v1/auth/logout` — novo (idempotente)
- `GET /api/v1/me` — novo, `RequireAuthorization("QualquerPerfil")`
- `GET /api/v1/admin/ping` — novo, endpoint de exemplo pra provar RBAC ponta a ponta, `RequireAuthorization("Admin")`

**Schema de banco (EF Core migrations):** 1 migration nova (`CriarIdentidade`) — schema `identidade` com tabelas `tenants`, `usuarios` (índice único global em e-mail), `refresh_tokens` (índice único em hash do token). Global Query Filter em `Usuario` por `tenant_id`.

## Métricas

| Métrica | Valor |
|---|---|
| Commits (PR #3 + hotfix PR #4) | 4 |
| Arquivos alterados | 65 (PR #3) + 2 (PR #4) |
| Linhas | +2.889/-17 (PR #3) + 3/-3 (PR #4) |
| Testes automatizados (backend) | 39 (fim do Sprint 0) → **96** (18 arquitetura + 63 unitários + 15 integração) |
| Testes automatizados (frontend) | 7 → **8** |
| Cobertura de testes (backend) | 84,21% (Sprint 0) → **83,6%** linha / 78,8% branch / 86,53% método (gate: 80% linha) |
| Vulnerabilidades (dotnet list --vulnerable) | 1 alta encontrada pelo CI pós-merge (SSH.NET/CVE do mesmo dia) → 0 após hotfix |
| QA manual (curl contra Postgres real) | Onboarding, login, bloqueio de conta, `/me`, RBAC (admin permitido/negado), refresh com rotação, logout — todos verificados ponta a ponta, ver `qa/processa-api-sprint1.http` |
| Rodadas de revisão | 1 (5 skills: code-review, senior-backend, senior-security, senior-frontend, find-bugs) |
| Migrations novas | 1 (`CriarIdentidade`) |
| Releases publicadas | v1.0.0-dev.2, v1.0.0-dev.3 |
| Bugs reais encontrados testando (antes do commit) | 5 |
| Bugs reais encontrados na revisão de 5 skills | 4 |
| Bugs reais encontrados pelo CI pós-merge | 1 (CVE de dependência transitiva) |

## Risco Técnico

- **Sem reuse-detection de refresh token** (Redis) — decisão consciente de escopo (ADR-005 completo ampliaria demais o sprint). O token é revogado a cada rotação e expira em 7 dias, mas um refresh token vazado e reutilizado antes da próxima rotação legítima não é detectado como anomalia. Registrado como pendência, não implementado pela metade.
- **Rate limiting global por IP ainda não existe** — o bloqueio de conta (5 tentativas → 15min) protege contra força bruta num e-mail específico, mas não limita um atacante tentando poucas senhas contra muitos e-mails diferentes (credential stuffing), nem o volume de requisições por IP. Avaliar `Microsoft.AspNetCore.RateLimiting` ou rate limiting na borda antes de produção.
- **Isolamento multi-tenant via Global Query Filter (coluna `tenant_id`)**, não schema/banco segregado por tenant — adequado pro MVP (ADR-002), mas exige disciplina de sempre usar o `DbContext` corretamente; reavaliar pra schema-per-tenant se um cliente piloto tiver exigência de compliance mais forte.
- **`ObterPorIdIgnorandoTenantAsync` é uma exceção documentada, mas ainda é uma exceção** — os únicos 2 pontos que legitimamente cruzam tenant (checagem de e-mail único, refresh de token) usam `IgnoreQueryFilters()` de forma explícita e nomeada; qualquer novo caso legítimo precisa do mesmo cuidado deliberado, não é automático.

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| Rate limiting global por IP em `/api/v1/auth/login` | Achado na revisão de segurança (find-bugs) | Avaliar `Microsoft.AspNetCore.RateLimiting` ou borda (gateway/CDN) antes de expor a API publicamente (Sprint 12) |
| Reuse-detection de refresh token via Redis | Escopo completo do ADR-005 ampliaria demais o Sprint 1 | Mover pra Redis com detecção de reuso quando o volume de sessões justificar |
| Mensagem de conta bloqueada revela existência do e-mail | Trade-off de UX vs. enumeração, aceito conscientemente | Revisitar se rate limiting por IP for implementado |
| Isolamento de tenant mais forte que Global Query Filter | ADR-002 escolheu coluna `tenant_id` pro MVP | Reavaliar pra schema-per-tenant se exigência de compliance aparecer antes do piloto (Sprint 12) |
| Jira desatualizado durante o sprint | Sessão retomou de contexto comprimido direto na depuração, sem reabrir o board antes | Corrigido a posteriori nesta sessão; manter o hábito de checar o Jira ao retomar trabalho |
