# Sprint 0 — Apresentação Técnica

Branch: `feature/processa-sprint0-fundacao` · Issue: PROJ-15 · PR #2

## 1. Entregas

| Feature/Fix | Status | Stack |
|---|---|---|
| Solução .NET 10, Clean Architecture (monólito modular) | OK | 1 `.csproj` por módulo, Domain/Application/Infrastructure/Presentation por namespace |
| Enforcement automatizado de arquitetura | OK | NetArchTest.Rules, 18 regras (Domain não depende de Application/Infrastructure/Presentation nem de MediatR/EF Core/ASP.NET Core) |
| Infra local via Docker Compose | OK | Postgres, Redis, RabbitMQ, MinIO — Dockerfiles multi-stage (backend: SDK→runtime, frontend: node build→nginx) |
| CI | OK | `dotnet format --verify-no-changes`, build, testes (3 projetos), gate de cobertura 80%, `dotnet list package --vulnerable`, `npm audit` |
| CD/Release | OK | semantic-release (Conventional Commits) + push de imagens Docker pro GHCR, branch protection (`main`/`develop`, status checks obrigatórios) |
| Frontend bootstrap | OK | React 19, Vite 8, TypeScript, Tailwind v4 (CSS-first), TanStack Query, oxlint, Vitest + Testing Library |
| Documentação interativa da API | OK | Swashbuckle.AspNetCore, `/swagger` |

## 2. Decisões de arquitetura

- **1 projeto por módulo, layering por namespace** (não 4 assemblies por módulo) — ADR-001 define as 4 camadas por módulo; a forma mais rígida seria 24 `.csproj` no total. Optado por um projeto por módulo com Domain/Application/Infrastructure/Presentation como pastas, validado por NetArchTest analisando namespace em vez de fronteira de assembly física — mesma garantia, muito menos overhead de solução pra um time de uma pessoa.
- **Gate de cobertura mede o total agregado** (`ThresholdStat=total`), não o mínimo por assembly — com `minimum`, o build reprovava mesmo em 84% de cobertura total porque um módulo ainda sem lógica de domínio real (Processos, só um handler de exemplo) ficava isolado abaixo de 80%. Reavaliar por-módulo quando todos os módulos tiverem lógica de domínio real.
- **Snyk via integração nativa do GitHub**, não CLI local — `npx snyk`/`snyk auth` exige fluxo de login interativo via browser, inviável numa sessão automatizada sem `SNYK_TOKEN` configurado. A integração GitHub escaneia sozinha a cada push/PR.
- **Vulnerabilidades do `package.json` raiz (tooling de release) aceitas como risco documentado** — `@semantic-release/npm` empacota uma cópia interna do npm com deps vulneráveis (brace-expansion, ip-address, tar, undici); é devDependency, nunca entra nas imagens Docker do produto, e o plugin nem está habilitado no `.releaserc.json` — código vulnerável nunca executa. `npm audit` da raiz por isso não é gate no CI (diferente de `backend/`/`frontend/`, que shipam pro usuário final).

## 3. Desvios do planejado

- **Alvo real é .NET 10, não .NET 8** — a documentação de especificação (escrita antes de qualquer código) citava "ASP.NET Core 8"; a máquina de desenvolvimento só tinha o SDK do .NET 10 (LTS atual) instalado no início do Sprint 0. Docs atualizadas para não ficarem desalinhadas do que está implementado.
- **CI/CD nível sênior não estava no escopo original da primeira passada** — o Sprint 0 inicialmente só entregou CI (build+testes). Ao ser perguntado diretamente se releases/CD também tinham sido construídos, a resposta honesta foi "não ainda" — e a pipeline completa (semantic-release + Docker/GHCR + branch protection) foi construída na sequência, dentro do mesmo Sprint 0, após confirmação explícita.

## 4. Bugs e resoluções

Achados pela passada das 5 skills de revisão (code-review, senior-backend, senior-frontend, senior-security, find-bugs) antes de abrir o PR:

- **CORS completamente ausente** — `dotnet run` + `npm run dev` pareciam funcionar (`curl` direto responde 200 normalmente, pois `curl` não reforça CORS), mas um `fetch()` real do browser (`localhost:5173` → `localhost:5000`) seria bloqueado. Só apareceu testando `OPTIONS` com header `Origin` de propósito. Fix: `AddCors`/`UseCors` com política nomeada, origens lidas de `Cors:AllowedOrigins`; verificado com preflight real (204 + `Access-Control-Allow-Origin`).
- **`Microsoft.OpenApi` com dependência transitiva vulnerável** — bump 2.0.0 → 2.11.0 até `dotnet list package --vulnerable` ficar limpo.
- **`.dockerignore` ausente** — build context de 144MB (incluindo `bin/`/`obj/`/`node_modules`) sendo enviado ao daemon Docker a cada build.
- **Portas inconsistentes**: `launchSettings.json` usava a porta padrão do template (5217), `httpClient.ts` já assumia 5000, o `readme.md` dizia vagamente "5xxx". Padronizado em 5000 (bate com o mapeamento do `docker-compose` do perfil `app`).
- **Swagger prometido na documentação, não implementado de verdade** — `docs/04-api/convencoes-api.md` já prometia Swashbuckle desde a especificação, mas o `Program.cs` só tinha `AddOpenApi()`/`MapOpenApi()` nativo do template (gera só o JSON, sem UI). Trocado por Swashbuckle real.
- **`react-router-dom`/`zustand` instalados sem nenhum uso** — dependência morta desde o bootstrap. Removidos; reinstalar exatamente quando a primeira tela que precisar deles nascer (Sprint 6, kanban).
- **`npm audit` ausente do job de frontend do CI** — adicionado como gate.

## 5. Código relevante

- `backend/tests/Processa.ArchitectureTests/CleanArchitectureTests.cs` — as 18 regras NetArchTest (Domain sem framework, Domain sem Application/Infrastructure/Presentation, Infrastructure/Presentation não vazam pra fora do módulo).
- `.github/workflows/ci.yml` — job `ci-success` como check estável, necessário porque os jobs `Backend`/`Frontend` são condicionais (via `dorny/paths-filter`) e branch protection não aceita bem checks que às vezes nem rodam.
- `.releaserc.json` — `develop` como branch `prerelease: "dev"`, `main` como estável; `semantic-release-export-data` pros outputs do GitHub Actions (nota: outputs usam nomes **hifenizados** `new-release-published`/`new-release-version`, não underscore — conferido lendo o código-fonte do plugin antes de confiar).

## 6. Bloqueios e dependências

Nenhum bloqueio externo — sprint de fundação técnica, sem dependência de terceiros (infra própria via Docker Compose local, sem serviço externo provisionado ainda).
