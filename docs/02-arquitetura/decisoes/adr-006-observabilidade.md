# ADR-006 — OpenTelemetry + Serilog como Padrão de Observabilidade

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Com múltiplos componentes assíncronos (API, workers, scheduler) processando a mesma demanda em momentos diferentes, depurar "por que essa notificação não chegou" exige rastrear uma requisição através de fronteiras de processo — não apenas logar dentro de cada componente isoladamente.

## Decisão

- **OpenTelemetry** para tracing distribuído: um `trace_id` gerado na requisição HTTP original é propagado através da publicação/consumo de mensagens RabbitMQ (via headers de mensagem), permitindo reconstruir a jornada completa "requisição → evento → worker → e-mail enviado" em uma única visualização.
- **Serilog** para logging estruturado (JSON), com enriquecimento automático de `tenant_id`, `user_id`, `trace_id` em todo log — nunca log de texto livre sem contexto.
- Exportação via OTLP para um backend compatível (Grafana Tempo/Loki, ou equivalente gerenciado) — a escolha do backend fica em aberto, mas a instrumentação do código é feita contra o padrão OpenTelemetry, não contra um vendor específico.
- Métricas de negócio (não só técnicas) expostas via OpenTelemetry Metrics: contagem de demandas por status, tempo médio por etapa, notificações enviadas/falhas — a mesma base de dados que alimenta os dashboards do produto (ver [requisitos, módulo 12](../../01-requisitos/requisitos-funcionais.md#12-módulo-dashboards-central-de-pendências-e-auditoria)) também alimenta observabilidade operacional.

## Alternativas consideradas

- **APM proprietário (Application Insights, Datadog) desde o início:** funcionalmente superior "out of the box", mas gera lock-in de vendor antes de o produto decidir sua infraestrutura de hospedagem definitiva. OpenTelemetry é exportável para qualquer um desses backends depois, sem reinstrumentar código.
- **Apenas logs, sem tracing distribuído:** insuficiente — a natureza assíncrona do sistema (fila → worker → integração externa) torna correlação manual de logs por timestamp inviável em produção com volume real.

## Consequências

- Instrumentação de tracing deve ser considerada desde o Sprint 0 (fundação técnica), não adicionada retroativamente — é muito mais barato instrumentar enquanto o código nasce do que depois.
- Overhead de performance do tracing é mitigado por sampling configurável (100% em desenvolvimento/staging, taxa reduzida em produção conforme volume).
