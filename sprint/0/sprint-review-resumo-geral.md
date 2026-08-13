# Sprint 0 — Resumo Geral

Sprint 0 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint0-fundacao` · PR #2

## Por Epic/Feature

| Feature | Status | Bloqueios |
|---|---|---|
| PROJ-15 — Fundação técnica (solução, Clean Architecture, Docker, CI/CD, frontend bootstrap) | Pronto — critérios batidos | Nenhum |

## Por Tecnologia

**Stack fixada:** .NET 10 (C# 13), EF Core 10 + Npgsql, MediatR 12, FluentValidation, React 19, Vite 8, TypeScript, Tailwind v4, TanStack Query.

**Infra:** Docker Compose com Postgres, Redis, RabbitMQ, MinIO. Dockerfiles multi-stage para `backend` (SDK build → runtime ASP.NET, usuário não-root) e `frontend` (node build → nginx com fallback de SPA e endpoint `/health`).

**Mudanças em contratos de API:** nenhuma ainda — sprint de fundação, sem endpoint de domínio real. `GET /health/live` é o único endpoint (health check).

**Schema de banco:** nenhuma migration ainda — nenhum módulo com entidade de domínio real neste sprint.

## Métricas

| Métrica | Valor |
|---|---|
| Arquivos alterados | 102 |
| Linhas | +5.270 / -12 |
| Testes automatizados (backend) | 39 (18 arquitetura + 19 unitários + 2 integração) |
| Testes automatizados (frontend) | 7 |
| Cobertura de testes (backend) | 84,21% linha / 76,92% branch / 87,5% método (gate: 80% linha) |
| Cobertura de testes (frontend) | Sem gate numérico ainda (ver Risco Técnico) |
| Vulnerabilidades (dotnet list --vulnerable) | 0 (após bump de `Microsoft.OpenApi`) |
| Vulnerabilidades (npm audit, backend/frontend shipáveis) | 0 |
| Vulnerabilidades (npm audit, tooling de release na raiz) | 7 (2 high, 5 moderate) — aceitas como risco documentado, ver Risco Técnico |
| Rodadas de revisão | 1 (5 skills: code-review, senior-backend, senior-frontend, senior-security, find-bugs) |
| Release publicada | v1.0.0-dev.1 (imagens Docker reais em `ghcr.io/ruaancorrea/processa-{api,frontend}`) |

## Risco Técnico

- **Gate de cobertura frontend ainda não existe** — o backend tem gate real de 80% (coverlet); o Vitest do frontend não tem threshold configurado. Decisão consciente: escrever testes reais onde há lógica que vale testar (ex.: `httpClient.test.ts`) em vez de perseguir um número artificial em cima de componentes ainda placeholder. Formalizar o gate quando entrar código de feature de verdade (Sprint 6+).
- **Vulnerabilidades do tooling de release (raiz) não são gate no CI** — `@semantic-release/npm` empacota uma cópia interna do npm com dependências vulneráveis, mas é devDependency que nunca entra nas imagens Docker e cujo plugin nem está habilitado no `.releaserc.json`. Risco residual real é ~zero, mas fica registrado para reavaliar se o semantic-release lançar uma versão que resolva isso upstream.
- **Sem serviço externo provisionado ainda** — Redis/RabbitMQ/MinIO existem só localmente via Docker Compose; nenhuma decisão de infra de produção (hosting, TLS, domínio) foi tomada neste sprint.

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| Estratégia de armazenamento de token no frontend | Login real ainda não existia neste sprint | Resolvida no Sprint 1 (cookie httpOnly + memória) |
| Middleware global de exceção (RFC 9457) | Nenhum endpoint de domínio lançava exceção de negócio ainda | Resolvida no Sprint 1 (`GlobalExceptionHandler`) |
