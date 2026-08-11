import { QueryClient } from "@tanstack/react-query";

/**
 * Cliente TanStack Query único para toda a aplicação — cache e revalidação
 * de dados do servidor. Ver docs/02-arquitetura/decisoes/adr-007-frontend-react.md
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: 1,
    },
  },
});
