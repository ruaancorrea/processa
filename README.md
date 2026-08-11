# Processa

**Gestão operacional inteligente para escritórios de contabilidade.**

Processa centraliza processos, tarefas, documentos e comunicação com clientes em uma única plataforma — o elo que normalmente falta entre o sistema contábil (fiscal/folha) e a rotina real da equipe, hoje espalhada em planilhas, grupos de WhatsApp e e-mails soltos.

> "O escritório sabe exatamente o que precisa ser feito, por quem, para qual cliente e até quando."

## O problema

Escritórios contábeis operam processos recorrentes e previsíveis — fechamento fiscal mensal, admissão de funcionário, abertura de empresa, entrega de obrigações acessórias — mas a maioria não tem uma ferramenta que:

- Modele esses processos como fluxos configuráveis, não como planilhas ad-hoc;
- Dê visibilidade gerencial de quem está atrasado, com quem e desde quando;
- Automatize follow-up de documentos pendentes com o cliente, sem depender de alguém lembrar de cobrar;
- Ofereça ao cliente final um portal simples para saber o que falta, sem precisar ligar para o escritório.

## A proposta

Processa é um motor de processos configurável (não um clone 1:1 de nenhum produto existente) com quatro pilares:

| Pilar | O que resolve |
|---|---|
| **Motor de processos** | Admin desenha o fluxo uma vez (etapas, responsáveis, prazos, dependências); o sistema executa e cobra a partir daí |
| **Kanban + lista operacional** | A equipe trabalha visualmente (arrastar card) ou em lista densa (filtrar/ordenar), sem digitar duas vezes a mesma informação |
| **Portal do cliente** | O cliente final envia documento, acompanha pendência e recebe cobrança automática, sem aprender o sistema interno |
| **Motor de regras** | Automação declarativa: "se X, então Y" — escalonamento, liberação de próxima etapa, alertas de vencimento |

Multi-tenant desde o desenho: cada escritório contábil é um tenant isolado, o que torna o produto vendável como SaaS, não apenas utilizável internamente por um único escritório.

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# 13 · ASP.NET Core 10 (LTS) (Minimal APIs + Controllers) |
| Arquitetura | Clean Architecture · Monólito modular por bounded context (DDD) |
| Banco de dados | PostgreSQL 16 · EF Core (Npgsql) |
| Cache / mensageria | Redis 7 · RabbitMQ (MassTransit) |
| Tempo real | SignalR (kanban colaborativo, notificações internas) |
| Frontend | React 19 + TypeScript · Vite 8 · TanStack Query · Tailwind CSS v4 · shadcn/ui |
| Observabilidade | OpenTelemetry (traces/métricas) · Serilog (logs estruturados) |
| Infra | Docker Compose · GitHub Actions (CI/CD) · Object Storage (S3-compatible) |
| Testes | xUnit + FluentAssertions + Testcontainers (backend) · Vitest + Testing Library (frontend) |

Ver justificativa de cada escolha em [`docs/02-arquitetura/decisoes/`](docs/02-arquitetura/decisoes/).

## Como rodar

```bash
cp .env.example .env        # preencher as senhas de Postgres/RabbitMQ/MinIO
docker compose up -d        # sobe postgres + redis + rabbitmq + minio, com health checks
```

```bash
# backend — API roda no host, falando com a infra containerizada
cd backend && dotnet run --project src/Processa.Api

# frontend — em outro terminal
cd frontend && npm install && npm run dev
```

Detalhes em [`backend/readme.md`](backend/readme.md) e [`frontend/readme.md`](frontend/readme.md).

## Documentação

| Documento | Conteúdo |
|---|---|
| [`docs/00-visao-geral/`](docs/00-visao-geral/visao-do-produto.md) | Visão de produto, problema, personas e jornadas |
| [`docs/01-requisitos/`](docs/01-requisitos/requisitos-funcionais.md) | Requisitos funcionais completos e catálogo de casos de uso |
| [`docs/02-arquitetura/`](docs/02-arquitetura/visao-arquitetural.md) | Visão arquitetural (C4), ADRs numerados |
| [`docs/03-modelagem/`](docs/03-modelagem/modelo-de-dominio.md) | Modelo de domínio (DDD), modelo de dados (ERD), máquina de estados do processo |
| [`docs/04-api/`](docs/04-api/convencoes-api.md) | Convenções de API REST e catálogo de endpoints |
| [`docs/05-seguranca/`](docs/05-seguranca/politica-de-seguranca.md) | Política de segurança, autenticação, LGPD |
| [`docs/06-uml/`](docs/06-uml/diagramas.md) | Diagramas de classe e sequência dos fluxos críticos |
| [`docs/07-roadmap/`](docs/07-roadmap/roadmap-mvp.md) | Roadmap do MVP e backlog por sprint |
| [`backlog/`](backlog/README.md) | Backlog completo (14 épicos → 13 sprints → 59 itens), rastreado no Jira |

## Estrutura do repositório

```
processa/
├── backend/                              # .NET 10 — ver backend/readme.md
│   ├── src/
│   │   ├── Processa.Api/                 # composition root, controllers, middlewares
│   │   ├── Processa.Modules.Identidade/  # tenants, usuários, auth, RBAC
│   │   ├── Processa.Modules.Clientes/    # clientes, contatos, grupos
│   │   ├── Processa.Modules.Processos/   # motor de processos (núcleo do domínio)
│   │   ├── Processa.Modules.Documentos/  # upload, versionamento, aprovação
│   │   ├── Processa.Modules.Notificacoes/# e-mail, WhatsApp, interno, tempo real
│   │   ├── Processa.Modules.Portal/      # API do portal do cliente
│   │   └── Processa.Shared.Kernel/       # building blocks DDD (Entity, ValueObject, DomainEvent)
│   └── tests/
│       ├── Processa.UnitTests/           # gate de cobertura 80% (ver .csproj)
│       ├── Processa.IntegrationTests/    # WebApplicationFactory, ponta a ponta
│       └── Processa.ArchitectureTests/   # NetArchTest — valida regras de dependência do ADR-001
├── frontend/                             # React + Vite — ver frontend/readme.md
│   └── src/
│       ├── features/                     # 1 pasta por bounded context, espelha o backend
│       └── shared/
├── docs/
├── backlog/
├── docker/                                 # Dockerfiles (backend, frontend) + nginx.conf
├── .github/workflows/                      # ci.yml (build/lint/test) + release.yml (semantic-release)
└── package.json                            # tooling de release (semantic-release) — não é o produto
```

## CI/CD

- **CI** ([`ci.yml`](.github/workflows/ci.yml)): build, `dotnet format`/oxlint, scan de vulnerabilidade (`dotnet list package --vulnerable` + `npm audit`), testes de arquitetura/unitários/integração, com path filters (só roda o job da pasta que mudou).
- **Release** ([`release.yml`](.github/workflows/release.yml)): [`semantic-release`](https://semantic-release.gitbook.io/) calcula a versão a partir de Conventional Commits — push em `develop` gera pre-release (`vX.Y.Z-dev.N`), push em `main` gera release estável (`vX.Y.Z`) com changelog automático no GitHub Releases e as imagens `ghcr.io/ruaancorrea/processa-api` e `ghcr.io/ruaancorrea/processa-frontend` publicadas.

Detalhes e alternativas consideradas em [ADR-010](docs/02-arquitetura/decisoes/adr-010-ci-cd-gitflow.md).

## Status

🏗️ **Sprint 0 concluído** — fundação técnica no ar: solução .NET em Clean Architecture com testes de arquitetura reais (NetArchTest) validando as regras do [ADR-001](docs/02-arquitetura/decisoes/adr-001-clean-architecture-modular-monolith.md), `docker compose up` sobe Postgres/Redis/RabbitMQ/MinIO com health checks, CI + release automático no GitHub Actions, frontend React com TanStack Query já falando com a API (CORS incluído). Próximo passo no roadmap: [Sprint 1 — Identidade e multi-tenancy](docs/07-roadmap/backlog-sprints.md#sprint-1--identidade-tenants-e-controle-de-acesso).

## Licença

[MIT](LICENSE) — código e documentação livres para estudo e reuso.
