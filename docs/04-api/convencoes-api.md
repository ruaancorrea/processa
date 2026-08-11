# Convenções de API

API REST, versionada por path (`/api/v1/...`), JSON como formato único de request/response. Documentação viva via OpenAPI/Swagger gerada a partir do código (Swashbuckle), publicada em `/swagger` (ambiente não-produção) e como referência estática em [`endpoints-principais.md`](endpoints-principais.md).

## 1. Recursos e rotas

- Nomes de recurso em português, plural, kebab-case: `/api/v1/tipos-processo`, `/api/v1/demandas`, `/api/v1/execucao-etapas`.
- Hierarquia por relação de posse: `/api/v1/demandas/{id}/execucao-etapas`, `/api/v1/execucao-etapas/{id}/comentarios`.
- Ações que não mapeiam a CRUD puro usam verbo como sub-recurso: `PATCH /api/v1/demandas/{id}/prioridade`, `POST /api/v1/demandas/{id}/cancelar`, `POST /api/v1/demandas/{id}/reabrir`.

## 2. Autenticação e autorização

- `Authorization: Bearer <jwt>` em toda rota exceto login/refresh/health.
- Rotas do portal do cliente vivem sob `/api/v1/portal/...`, com o esquema de autenticação separado descrito no [ADR-005](../02-arquitetura/decisoes/adr-005-autenticacao-multi-perfil.md) — nunca aceitam token do esquema interno.
- Tokens de API (integrações externas) autenticam via `Authorization: Bearer <token-api>` e são diferenciados do JWT de usuário pelo prefixo (`pk_live_...`); toda rota valida o escopo necessário (`processos:read`, `processos:write`, etc.) além da autenticação.

## 3. Paginação, filtro e ordenação

- Paginação obrigatória em toda listagem: `?page=1&pageSize=20` (padrão 20, máximo 100).
- Resposta paginada sempre no envelope:
```json
{
  "data": [ ... ],
  "meta": { "page": 1, "pageSize": 20, "total": 143, "totalPages": 8 }
}
```
- Filtros como query string nomeada: `?status=em_andamento&prioridade=alta&clienteId=...`.
- Ordenação: `?sort=prioridade:desc,dataInicio:asc` (suporta múltiplos critérios, na ordem informada).

## 4. Formato de erro

Todo erro segue [RFC 9457 (Problem Details)](https://www.rfc-editor.org/rfc/rfc9457):

```json
{
  "type": "https://processa.app/erros/validacao",
  "title": "Erro de validação",
  "status": 422,
  "detail": "O campo 'cliente_id' é obrigatório.",
  "errors": { "cliente_id": ["campo obrigatório"] },
  "traceId": "00-4bf92f...-00"
}
```

`traceId` corresponde ao `trace_id` do OpenTelemetry (ver [ADR-006](../02-arquitetura/decisoes/adr-006-observabilidade.md)) — permite correlacionar o erro reportado ao usuário com o rastro completo da requisição nos logs.

## 5. Versionamento e compatibilidade

- Mudança breaking → nova versão de path (`/api/v2/...`), versão anterior mantida por período de depreciação anunciado.
- Campo novo opcional em resposta existente não é breaking — adicionado livremente.
- Enums nunca têm valor removido sem depreciação — apenas adicionados (cliente antigo que não reconhece um valor novo deve degradar graciosamente, não quebrar).

## 6. Idempotência

- Toda rota de criação (`POST`) que pode ser reenviada por retry de rede aceita header `Idempotency-Key`; reenvio com a mesma chave dentro de 24h retorna a resposta original, sem duplicar o efeito.
- Rotas de automação disparadas por regras internas usam a mesma prática para evitar duplicidade em caso de reprocessamento de mensagem.

## 7. Real-time (SignalR)

- `/hubs/kanban` — grupo por `equipe_id`, eventos: `CardMovido`, `ResponsavelAlterado`.
- `/hubs/notificacoes` — grupo por `usuario_id`, evento: `NovaNotificacao`.
- Autenticação via JWT na negociação de conexão (`?access_token=...`), documentado explicitamente em [ADR-009](../02-arquitetura/decisoes/adr-009-notificacoes-tempo-real-signalr.md).

Ver o catálogo de endpoints em [`endpoints-principais.md`](endpoints-principais.md).
