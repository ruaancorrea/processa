# ADR-004 — RabbitMQ + MassTransit para Eventos e Automações

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Várias operações não podem bloquear a resposta HTTP: envio de e-mail/WhatsApp, chamada a API externa (etapa Automatizada), avaliação do motor de regras, criação de evento no Google Calendar. Precisam de retry com backoff, dead-letter queue para falhas persistentes, e filas segregadas por criticidade.

## Decisão

**RabbitMQ** como broker, consumido via **MassTransit** (abstração de mensageria idiomática para .NET, evita acoplamento direto ao cliente RabbitMQ e facilita testes com o transporte in-memory do próprio MassTransit).

Filas segregadas por domínio:
- `fila-notificacoes` — envio de e-mail, WhatsApp, notificação interna.
- `fila-automacoes` — chamadas de etapa Automatizada, avaliação do motor de regras.
- `fila-calendario` — integração Google Calendar.

Retry automático com backoff exponencial (3 tentativas: 60s, 300s, 900s), dead-letter queue por fila para falha persistente, visível em um painel administrativo simples de "mensagens com falha" (Fase 2).

## Alternativas consideradas

- **Azure Service Bus / AWS SQS:** adequados, mas atrelam a infraestrutura a um cloud provider específico antes de o produto ter decidido onde hospedar definitivamente; RabbitMQ roda igualmente bem on-premise, em Docker Compose local, ou em qualquer cloud.
- **Hangfire (jobs em banco, sem broker dedicado):** suficiente para jobs agendados simples, mas insuficiente como barramento de eventos entre módulos (não é um message broker real, é um scheduler+worker sobre o próprio banco). Usado de forma complementar apenas para o **Scheduler** (Quartz.NET, ver visão arquitetural) que dispara verificações periódicas (SLA, lembretes).

## Consequências

- Um módulo publica um evento de domínio sem saber quem consome — desacoplamento real entre `Modules.Processos` e `Modules.Notificacoes`.
- Exige RabbitMQ como dependência de infraestrutura desde o Sprint 0 (já presente no `docker-compose.yml` local).
- Consistência é eventual entre a ação do usuário e o efeito assíncrono (ex: e-mail chega alguns segundos depois) — aceitável para o domínio, documentado explicitamente nos requisitos para não gerar expectativa de sincronicidade.
