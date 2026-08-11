# ADR-009 — SignalR para Tempo Real (Kanban, Notificações Internas)

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Dois cenários exigem atualização em tempo real sem polling: (1) o kanban precisa refletir instantaneamente quando outro usuário move um card, e (2) a central de notificações internas precisa avisar o usuário sem que ele precise recarregar a página.

## Decisão

**SignalR** (nativo do ecossistema ASP.NET Core) com **Redis como backplane** — necessário porque a API roda potencialmente em múltiplas instâncias atrás de um load balancer (arquitetura stateless, ver visão arquitetural), e o backplane garante que uma mensagem publicada a partir da instância A chegue a um cliente conectado à instância B.

Dois hubs:
- `KanbanHub` — grupo por `equipe_id`; eventos de movimentação de card, mudança de responsável.
- `NotificacoesHub` — grupo por `usuario_id`; eventos de nova notificação interna.

Conexão autenticada com o mesmo JWT da API REST (SignalR suporta autenticação via query string do token na negociação de conexão, documentado explicitamente para evitar a armadilha comum de esquecer esse detalhe).

## Alternativas consideradas

- **Polling curto (ex: a cada 5s) via TanStack Query:** simples, mas gera carga desnecessária no backend proporcional ao número de usuários simultâneos, e a experiência de "quase tempo real" é pior que a de um kanban colaborativo de verdade.
- **Server-Sent Events (SSE):** alternativa mais simples que WebSocket para o caso de notificações (unidirecional), mas SignalR já cobre esse caso com fallback automático de transporte e reconexão, sem precisar manter dois mecanismos diferentes (um para kanban bidirecional, outro para notificações).

## Consequências

- Redis passa a ser dependência não apenas de cache, mas também de infraestrutura de tempo real — reforça a decisão de tê-lo desde o Sprint 0.
- Testes de integração de SignalR exigem um harness dedicado (`TestServer` + `HubConnection` em memória) — catalogado em `.claude/testing.md`-equivalente do projeto quando o código nascer.
