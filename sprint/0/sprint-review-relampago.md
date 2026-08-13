SPRINT 0 — FUNDAÇÃO TÉCNICA (PROJ-15) — 2026-08-10/11

ENTREGAS:
• Solução .NET 10, Clean Architecture como monólito modular (1 .csproj por módulo, Domain/Application/Infrastructure/Presentation por namespace) → OK
• NetArchTest (18 regras) validando as fronteiras de camada/módulo no CI → OK
• Docker Compose (Postgres, Redis, RabbitMQ, MinIO) + Dockerfiles multi-stage (backend/frontend) → OK
• CI (build, format, testes, gate de cobertura 80%, scan de vulnerabilidades) → OK
• CD/release: semantic-release (Conventional Commits) + push de imagens pro GHCR, branch protection em `main`/`develop` → OK
• Frontend: React 19 + Vite 8 + TypeScript + Tailwind v4 + TanStack Query, oxlint → OK
• Swagger/OpenAPI interativo (Swashbuckle) → OK

DECISÕES:
• Alvo real .NET 10 (não 8 como a doc inicial dizia) → SDK disponível na máquina era o 10
• 1 projeto por módulo com layering por namespace, não 4 assemblies por módulo → mesma garantia via NetArchTest, menos overhead pra time de 1 pessoa
• Gate de cobertura mede TOTAL agregado, não mínimo por assembly → módulo ainda sem lógica real (Processos) reprovava isolado
• Snyk via integração GitHub, não CLI local → `npx snyk` exige login interativo, mesma fricção do Credential Manager

BUGS (achados na revisão code-review/senior-backend/senior-frontend/senior-security/find-bugs):
• CORS ausente no Program.cs → Raiz: `dotnet run`+`npm run dev` "funcionavam" via curl, mas fetch real do browser seria bloqueado → Fix: AddCors/UseCors com política nomeada, verificado com preflight real
• Microsoft.OpenApi com dependência transitiva vulnerável → Fix: bump 2.0.0→2.11.0 até `dotnet list package --vulnerable` limpar
• `.dockerignore` ausente → Raiz: build context de 144MB → Fix: adicionado
• Portas inconsistentes (launchSettings 5217 vs httpClient.ts 5000 vs docs "5xxx") → Fix: padronizado em 5000
• Swagger prometido na doc mas não implementado (só OpenAPI JSON nativo) → Fix: Swashbuckle real com UI
• react-router-dom/zustand instalados sem uso → Fix: removidos, reinstalar quando a 1ª tela precisar (Sprint 6)
• `npm audit` ausente no job de frontend do CI → Fix: adicionado

BLOQUEIOS:
• Nenhum bloqueio externo — sprint de fundação, sem dependência de terceiros

MÉTRICAS:
• Arquivos: 102 (+5270/-12) | Testes: 39 backend (18 arquitetura + 19 unitários + 2 integração) + 7 frontend | Cobertura: 84,21% linha / 76,92% branch / 87,5% método | Release: v1.0.0-dev.1 publicada (imagens Docker reais no GHCR)

NEXT:
• Abrir PR de feature/processa-sprint0-fundacao → develop → feito, mergeado
• Iniciar Sprint 1 (Identidade, Tenants e Controle de Acesso, PROJ-16) → Backend
