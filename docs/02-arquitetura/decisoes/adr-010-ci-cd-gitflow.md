# ADR-010 — GitFlow Leve + Conventional Commits + CI Obrigatório

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

O projeto precisa de um fluxo de trabalho que produza histórico legível, releases previsíveis e qualidade verificada automaticamente — sem impor um processo pesado desproporcional ao tamanho da equipe inicial.

## Decisão

- **GitFlow leve:** `main` (produção, sempre estável) ← `develop` (integração) ← `feature/processa-<área>-<resumo>` / `fix/...`. Toda branch de feature nasce a partir da `develop` atualizada.
- **Conventional Commits** (`feat:`, `fix:`, `refactor:`, `test:`, `docs:`, `chore:`, `ci:`, `build:`) — versão semântica calculada automaticamente a partir do histórico: `BREAKING CHANGE`/`!` → major, `feat` → minor, demais → patch.
- **CI obrigatório no GitHub Actions** antes de merge: build, `dotnet format --verify-no-changes`, scan de dependência vulnerável (`dotnet list package --vulnerable`), testes unitários (xUnit, cobertura mínima 80%), de integração e de arquitetura (NetArchTest). PR não mergeável com CI vermelho (`.github/workflows/ci.yml`).
- **Release automático via `semantic-release`** (`.github/workflows/release.yml`, `.releaserc.json`): push em `develop` → tag e pre-release GitHub (`vX.Y.Z-dev.N`); push em `main` → release estável (`vX.Y.Z`) + build e push da imagem `ghcr.io/ruaancorrea/processa-api` (tag da versão + `latest`/`dev` mutável). Depende de branch protection exigindo o check de CI antes do merge — configurar em Settings → Branches do repositório.
- TDD como prática esperada (não só cobertura mínima): funcionalidade nasce com teste — Red → Green → Refactor — documentado como regra de desenvolvimento do projeto.

## Alternativas consideradas

- **Trunk-based development:** mais moderno para equipes que já praticam deploy contínuo com feature flags maduras; descartado para o estágio inicial porque a disciplina de feature flags ainda não existe no produto, e GitFlow leve dá uma rede de segurança mais simples de operar sozinho ou em equipe pequena.
- **Sem gate de cobertura no CI:** rejeitado — cobertura mínima é a única garantia objetiva, não-subjetiva, de que testes acompanham o código; deixá-la "recomendada" na prática significa que ela nunca acontece sob pressão de prazo.

## Consequências

- Todo PR carrega o custo de rodar o pipeline completo — aceito como parte do custo de manter qualidade previsível, não como atrito a ser contornado.
- Regras completas de commit, branch e release ficam documentadas no `CONTRIBUTING.md` do repositório, não apenas neste ADR.
