SPRINT 4 — MOTOR DE PROCESSOS II: OS 8 TIPOS DE ETAPA — 2026-08-18

ENTREGAS:
• Entidade Etapa (CRUD dentro de um Fluxo, ordenada) → OK
• Etapa Comum e Conclusão → OK
• Etapa Condicional (desdobramento por valor de campo) → OK
• Etapa Automatizada (chamada HTTP real, único handler que já executa de verdade) → OK
• Etapa de Notificação e Etapa de Agendamento → OK (envio real de notificação é Sprint 8)
• Etapa de Subprocesso e Etapa de União (fork/join) → OK como lógica de decisão; persistência real é Sprint 5
• Configuração de acesso por etapa (quem pode alterar/concluir) → OK

DECISÕES:
• Handlers ficam no Domain, mas dependem de interfaces (IResolvedorValorCampo,
  INotificadorEtapa, ICriadorSubprocesso, IVerificadorDesdobramentos) implementadas
  como stubs que falham alto até o Sprint 5 → mantém a lógica de decisão real e
  testável sem fingir que a orquestração completa já existe
• Configuracao da Etapa é polimórfica via [JsonPolymorphic]/[JsonDerivedType]
  (System.Text.Json, BCL) → evita um DTO achatado com um campo por tipo

BUGS:
• GET /etapas/{id} de uma etapa Automatizada devolvia 500 → Raiz: coluna jsonb no
  Postgres não preserva ordem de chave, e leitura polimórfica do System.Text.Json
  exige o discriminador como primeira propriedade → Fix: coluna trocada pra "json"
  (preserva o texto literal)
• CriarCampoPersonalizadoCommand [Sprint 3, achado revisitado] não tinha teste
  cobrindo a regra "Lista exige opções" com formato RFC 9457 completo → já corrigido
  na Sprint 3, mantido aqui como lembrete de padrão
• CriarEtapaCommand/AtualizarEtapaCommand sem espelho FluentValidation da validação
  interna de cada Configuracao (URL válida, ramos não-vazios etc.) → Raiz: mesma
  classe recorrente RFC 9457 → Fix: validator reaproveita ConfiguracaoEtapa.Validar()
  (mesmo método do Domain), sem duplicar regra

BLOQUEIOS:
• Nenhum bloqueio real. Riscos documentados (não bloqueantes): referências
  etapa-a-etapa não validadas contra etapas reais; Etapa Automatizada é vetor de
  SSRF em potencial quando o Sprint 5 ligar a execução de verdade — ver
  .faf/pendencias.faf

MÉTRICAS:
• Arquivos: 44 | Testes: 272→348 (24 arquitetura + 259 unitários + 65 integração) | Cobertura: 83,3%→83,46%

NEXT:
• Sprint 5 — Execução de Demandas (Demanda, ExecucaoEtapa, orquestrador real dos
  handlers, herança de campos do Subprocesso, fork/join de verdade) — só ao "segue"
  explícito
