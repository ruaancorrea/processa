SPRINT 5 — EXECUÇÃO DE DEMANDAS — 2026-08-19

ENTREGAS:
• Demanda + ExecucaoEtapa + orquestrador real dos 8 handlers do Sprint 4 → OK
• Abertura por formulário, API (mesmo endpoint) e subprocesso automático → OK
• Atribuição de responsável nos 3 modos (fixo/dinâmico/manual) → OK
• Fork/join REAL (paralelismo genuíno, não simulado) → OK
• Comentários, anexos (MinIO real, não stub) e histórico auditável → OK
• PermissoesInicio (Sprint 3) e mitigação de SSRF (Sprint 4) finalmente resolvidas → OK

DECISÕES:
• Fork/join real em vez de União só-esperar-conclusão → decisão explícita do
  usuário, mudou contrato já mergeado (ProximaEtapaId → ProximasEtapasIds)
• MinIO real via AWSSDK.S3 em vez de stub → decisão explícita do usuário
• ArmazenamentoArquivoS3 virou Singleton (era Scoped) → AmazonS3Client é
  caro de construir e thread-safe, mesma recomendação da AWS

BUGS:
• 3 bugs reais no orquestrador (achados via TDD, single-threaded) → Raiz:
  gatilho de União errado, ramo de fork caindo em travessia linear, União
  eager-criada nunca reavaliada → Fix: os 3 corrigidos antes de qualquer
  exposição HTTP
• Upload de anexo 500 no MinIO novo → Raiz: AWSSDK.S3 v4 retorna
  ListBucketsAsync().Buckets null (não vazio) em conta sem bucket → Fix:
  checagem null-safe (achado em QA manual real, não unit test)
• Corrida no primeiro-a-chegar numa União nova (achado em revisão find-bugs)
  → Raiz: perdedor da corrida de índice único perde o desdobramento junto
  → NÃO corrigido agora (risco de bug pior sem teste de carga) → documentado
  em .faf/pendencias.faf com plano de mitigação

BLOQUEIOS:
• Nenhum bloqueio real. Um risco documentado (não bloqueante): corrida rara
  no fork/join — ver .faf/pendencias.faf

MÉTRICAS:
• Arquivos: 89 | Testes: 348→473 (24 arquitetura + 373 unitários + 76 integração) | Cobertura: 83,46%→83,71%

NEXT:
• Sprint 6 — Kanban e Painel Operacional — só ao "segue" explícito
