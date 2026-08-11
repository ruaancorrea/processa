# Contribuindo

## Fluxo de trabalho

1. Branch sempre a partir da `develop` atualizada: `feature/processa-<área>-<resumo>`, `fix/...`, `hotfix/...`.
2. Commits semânticos ([Conventional Commits](https://www.conventionalcommits.org/)): `feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `ci:`, `build:`.
3. Ao final das alterações: push e PR para `develop`, com CI verde como pré-requisito de merge.

## Qualidade

- **TDD:** toda funcionalidade nasce com teste — Red → Green → Refactor. Backend: xUnit + FluentAssertions, cobertura mínima 80% (gate de CI). Frontend: Vitest + Testing Library.
- **Clean Architecture:** regras de dependência do [ADR-001](docs/02-arquitetura/decisoes/adr-001-clean-architecture-modular-monolith.md), validadas por testes de arquitetura automáticos — uma violação quebra o build.
- **Documentação viva:** qualquer mudança estrutural relevante atualiza a documentação correspondente (`docs/`) no mesmo PR — ADR novo para toda decisão arquitetural significativa.

## Versionamento e releases

SemVer calculado a partir dos commits convencionais (`BREAKING CHANGE`/`!` → major, `feat` → minor, demais → patch). Merge em `develop` gera pre-release (`vX.Y.Z-dev.N`); merge em `main` gera release estável. Detalhes em [ADR-010](docs/02-arquitetura/decisoes/adr-010-ci-cd-gitflow.md).

## Onde propor mudanças de arquitetura

Mudança relevante de estrutura (nova dependência central, novo bounded context, mudança de schema fora de migration aditiva) exige um ADR novo em [`docs/02-arquitetura/decisoes/`](docs/02-arquitetura/decisoes/) antes da implementação — não depois.
