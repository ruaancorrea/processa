# ADR-008 — Object Storage S3-compatible para Documentos

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Documentos e anexos (upload de comentário de etapa, documentos do portal do cliente) variam de KBs a dezenas de MBs, com necessidade de versionamento e possível volume alto por tenant ao longo do tempo. Armazenar binário em coluna do PostgreSQL (`bytea`) infla o banco, degrada performance de backup/restore e de replicação.

## Decisão

Documentos armazenados em **object storage S3-compatible** (MinIO em desenvolvimento/on-premise; AWS S3 ou equivalente em produção cloud) — o código depende apenas da API S3, não de um provedor específico, via `IDocumentStorageService` abstraindo o SDK.

- Nome armazenado gerado como UUID (evita colisão, evita expor estrutura interna), nome original preservado em metadado para exibição.
- Versionamento de documento modelado explicitamente no domínio (`DocumentoVersao`), não delegado ao versionamento nativo do bucket S3 — permite listar/reverter versões pela aplicação sem acoplar a lógica de negócio a uma feature específica do provedor de storage.
- URLs de download geradas como **pre-signed URLs** de curta duração (poucos minutos) — a API nunca faz proxy do binário, o cliente baixa diretamente do storage, e o link expira, exigindo nova autorização a cada download.

## Alternativas consideradas

- **Banco relacional (`bytea`/`large object`):** rejeitado para volume de produção — adequado só para protótipo descartável.
- **Sistema de arquivos local do container:** rejeitado — não sobrevive a redeploy/scaling horizontal da API (viola o requisito de arquitetura stateless).

## Consequências

- Object storage é uma dependência de infraestrutura obrigatória desde o Sprint 0 (MinIO no `docker-compose.yml` local, para paridade dev/produção).
- Pre-signed URLs exigem que o cliente consumidor da API trate o fluxo de download em duas etapas (pedir URL assinada → baixar do storage), não um único endpoint que serve o binário.
