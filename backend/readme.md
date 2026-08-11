# Backend — Processa.Api

.NET 10 (LTS) · Clean Architecture · monólito modular por bounded context. Ver [ADR-001](../docs/02-arquitetura/decisoes/adr-001-clean-architecture-modular-monolith.md) para a justificativa completa e as regras de dependência.

## Estrutura

```
backend/
├── Processa.slnx
├── src/
│   ├── Processa.Api/                 # composition root — Program.cs, wiring de DI, endpoints
│   ├── Processa.Shared.Kernel/       # Entity, ValueObject, IDomainEvent, Result, Specification
│   └── Processa.Modules.*/           # um por bounded context (Domain/Application/Infrastructure/Presentation)
└── tests/
    ├── Processa.UnitTests/           # testes de domínio e aplicação, com gate de cobertura 80%
    ├── Processa.IntegrationTests/    # WebApplicationFactory — testa a API de ponta a ponta
    └── Processa.ArchitectureTests/   # NetArchTest — valida as regras de dependência do ADR-001
```

## Como rodar

```bash
# 1. Suba a infra (Postgres, Redis, RabbitMQ, MinIO)
cp .env.example .env   # preencha as senhas
docker compose up -d

# 2. Rode a API no host, apontando pra infra containerizada
cd backend
dotnet run --project src/Processa.Api
```

API disponível em `http://localhost:5xxx` (porta definida pelo launch profile), com `/health/live` para checagem rápida e `/swagger` (ambiente Development) para a documentação OpenAPI.

## Testes

```bash
cd backend
dotnet test                              # roda tudo: unit + integration + architecture
dotnet test tests/Processa.UnitTests     # só unit — aplica o gate de cobertura 80% (ver .csproj)
```

## Qualidade

```bash
dotnet format Processa.slnx --verify-no-changes   # o mesmo check que roda no CI
dotnet list Processa.slnx package --vulnerable --include-transitive
```

Regras completas de qualidade e fluxo de trabalho em [`../CONTRIBUTING.md`](../CONTRIBUTING.md).
