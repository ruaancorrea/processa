# ADR-002 — Multi-tenancy por Coluna com Global Query Filters

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Processa é pensado como produto vendável a múltiplos escritórios de contabilidade (tenants), não como sistema interno de um único cliente. Cada tenant precisa de isolamento total de dados: um escritório jamais pode ver clientes, processos ou usuários de outro.

## Decisão

**Banco de dados compartilhado, isolamento por coluna `tenant_id`.** Toda tabela com dado pertencente a um tenant tem uma coluna `tenant_id UUID NOT NULL`, indexada, e o EF Core aplica um **Global Query Filter** (`HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)`) automaticamente em todo `DbSet` relevante — nenhum desenvolvedor precisa lembrar de filtrar manualmente, e esquecer o filtro é estruturalmente impossível sem usar `IgnoreQueryFilters()` explicitamente (o que é sinalizado em code review).

`ITenantContext` resolve o tenant atual a partir do token JWT (claim `tenant_id`) em um middleware, antes de qualquer acesso a dado.

## Alternativas consideradas

- **Schema por tenant (mesmo banco, schema PostgreSQL isolado):** mais isolamento, mas complica migrations (aplicar em N schemas) e connection pooling. Reavaliar se surgir requisito de compliance que exija isolamento físico mais forte.
- **Banco por tenant:** isolamento máximo, mas custo operacional alto (provisionamento, backup, migrations por banco) incompatível com um MVP que ainda não sabe seu número final de tenants. Correto para uma fase de escala avançada, não para o MVP.

## Consequências

- Query esquecida sem filtro é a superfície de risco nº 1 deste modelo — mitigada pelo Global Query Filter ser automático, não opt-in.
- Índice composto `(tenant_id, <coluna de busca frequente>)` é padrão em todas as tabelas — ver [modelo de dados](../../03-modelagem/modelo-de-dados.md).
- Métricas e logs devem carregar `tenant_id` como dimensão desde o Sprint 0, para permitir observabilidade por tenant.
- Migração para isolamento mais forte (schema ou banco por tenant) permanece possível: o `tenant_id` já particiona logicamente os dados, então a migração seria de infraestrutura, não de modelo.
