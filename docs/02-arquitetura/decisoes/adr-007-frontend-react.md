# ADR-007 — React + TypeScript + Vite no Frontend, sem SSR no MVP

**Status:** ~~Aceito~~ **Superseded** · **Data:** 2026-08-10 · **Revertido em:** 2026-08-22

> **Nota de reversão:** Processa foi reposicionado como projeto **API-only** — o frontend
> descrito aqui foi implementado (Sprint 6 chegou a entregar autenticação, kanban e painel
> operacional) e depois removido do repositório para manter o escopo focado em arquitetura
> e engenharia de backend. Decisão mantida como registro histórico; não reflete o estado
> atual do código. A API continua expondo os mesmos dados (kanban por etapa, filtros,
> paginação) para que qualquer cliente HTTP os consuma.

## Contexto

O frontend tem duas superfícies muito diferentes: um painel operacional denso (kanban, tabelas, filtros — pensado para uso autenticado intenso, não para SEO) e um portal de cliente simples (poucas telas, também autenticado). Nenhuma das duas precisa de renderização no servidor para indexação em buscador.

## Decisão

**React 18 + TypeScript, buildado com Vite**, SPA pura (client-side rendering), sem framework full-stack (Next.js) no MVP — já que não há necessidade de SSR/SSG e um bundler mais simples reduz superfície de configuração.

- **TanStack Query** para cache e revalidação de dados do servidor.
- **shadcn/ui + Tailwind CSS** para componentes — base de componentes acessíveis, customizável sem framework de UI pesado.
- **Zustand** para estado de UI local que não pertence ao servidor (ex: estado do drag-and-drop do kanban antes de persistir).
- Estrutura de pastas por *feature*, espelhando os bounded contexts do backend (`features/processos/`, `features/clientes/`, `features/kanban/`...) — facilita a um desenvolvedor navegar do módulo de backend ao código de frontend correspondente.
- Portal do cliente é uma **aplicação React separada** (não apenas uma rota protegida dentro do mesmo SPA), com seu próprio bundle, deploy e domínio (`cliente.processa.app` vs `app.processa.app`) — reforça o isolamento de identidade decidido no [ADR-005](adr-005-autenticacao-multi-perfil.md) e evita que código/bundle do painel interno vaze para uma superfície pública.

## Alternativas consideradas

- **Next.js (App Router):** avaliado e descartado para Processa porque nenhuma tela do produto precisa de SSR/SEO, e o custo de aprendizado/configuração adicional (Server Components, streaming, roteamento de arquivo) não se paga sem esse requisito. Reavaliar se surgir uma landing page pública de marketing que precise de SEO — nesse caso, ela seria um site estático separado, não o painel autenticado.
- **Blazor (mantendo tudo em .NET):** avaliado pela sinergia com o backend, mas o ecossistema de componentes ricos para kanban/tabelas densas é mais maduro em React; e um frontend em TypeScript mantém empregabilidade e portfólio mais alinhados ao mercado.

## Consequências

- Dois bundles de frontend (painel interno + portal do cliente) para manter, mas com ganho real de isolamento de segurança e de bundle size (o portal não carrega código do kanban que o cliente jamais usa).
- Sem SSR, o primeiro carregamento depende de um loading state bem desenhado — tratado como requisito de UX, não deixado como afterthought.
