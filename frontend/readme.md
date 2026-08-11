# Frontend — Processa

React 19 + TypeScript + Vite 8. SPA client-side, sem SSR — justificativa em [ADR-007](../docs/02-arquitetura/decisoes/adr-007-frontend-react.md).

## Estrutura

```
frontend/src/
├── features/        # 1 pasta por bounded context, espelha os módulos do backend
│   ├── identidade/
│   ├── clientes/
│   ├── processos/
│   ├── kanban/
│   ├── documentos/
│   ├── notificacoes/
│   └── portal/
├── shared/
│   ├── api/          # httpClient (fetch + tratamento de erro RFC 9457) e queryClient (TanStack Query)
│   ├── components/    # componentes reutilizáveis entre features
│   └── layout/        # AppShell — navegação e moldura da aplicação
└── test/              # setup do Vitest (jest-dom)
```

## Como rodar

```bash
cd frontend
npm install
npm run dev          # http://localhost:5173, falando com a API em VITE_API_BASE_URL (padrão http://localhost:5000)
```

## Qualidade

```bash
npm run lint          # oxlint
npm run test -- --run # Vitest + Testing Library
npm run build          # tsc -b && vite build — o mesmo que roda no CI
```

## Stack

TanStack Query (cache/revalidação de dados do servidor) · Zustand (estado de UI local) · Tailwind CSS v4 · React Router. Base de componentes acessíveis (shadcn/ui) a integrar conforme as telas forem construídas a partir do Sprint 6 — ver [roadmap](../docs/07-roadmap/roadmap-mvp.md).
