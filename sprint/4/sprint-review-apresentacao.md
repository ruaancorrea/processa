# Sprint 4 — Apresentação

Sprint 4 do cronograma (`docs/07-roadmap/backlog-sprints.md`) — Motor de Processos II: os 8 Tipos de Etapa · Board PROJ (PROJ-19, subtasks PROJ-45 a PROJ-51) · Branch `feature/processa-sprint4-motor-processos-etapas`

## 1. Entregas

| Feature | Status | Stack |
|---|---|---|
| Entidade `Etapa` (CRUD dentro de um Fluxo, ordenada) | Pronto | Domain, Application (MediatR), EF Core/Postgres |
| Etapa Comum e Etapa de Conclusão | Pronto | `EtapaComumHandler` (scaffold Sprint 0), `EtapaConclusaoHandler` |
| Etapa Condicional (desdobramento por campo personalizado) | Pronto | `ConfiguracaoEtapaCondicional`, `EtapaCondicionalHandler` |
| Etapa Automatizada (chamada HTTP + validação de resposta) | Pronto e **executável de verdade** | `ConfiguracaoEtapaAutomatizada`, `EtapaAutomatizadaHandler`, `ClienteHttpEtapa` (`IHttpClientFactory` real) |
| Etapa de Notificação e Etapa de Agendamento | Pronto como configuração/decisão | `ConfiguracaoEtapaNotificacao`/`ConfiguracaoEtapaAgendamento`, respectivos handlers |
| Etapa de Subprocesso (herança de campos do pai) | Pronto como configuração/decisão | `ConfiguracaoEtapaSubprocesso`, `EtapaSubprocessoHandler` |
| Etapa de União (fork/join) | Pronto como configuração/decisão | `ConfiguracaoEtapaUniao`, `EtapaUniaoHandler` |
| Configuração de acesso por etapa (quem pode alterar/concluir) | Pronto | VO `ConfiguracaoAcessoEtapa`, `DefinirConfiguracaoAcessoEtapaCommand` |

## 2. Decisões de arquitetura

**Escopo do sprint: configuração + lógica de decisão real, não orquestração completa.** ADR-003 desenha `IEtapaHandler.ExecutarAsync` operando sobre `DemandaId`/`ExecucaoEtapaId` — conceitos que só existem a partir do Sprint 5 (`demandas`, `execucao_etapas`, `desdobramentos_aguardados` ainda não existem). Decisão (documentada em `.faf/decisions.faf`): Sprint 4 entrega a entidade `Etapa` com configuração validada por tipo, e os 8 `IEtapaHandler` como lógica de domínio real e testável via um contexto sintético (`ExecucaoEtapaContexto`) — não contra uma `Demanda` persistida de verdade. Onde um handler precisa de dado que só existe em execução real (União checando irmãos, Subprocesso disparando um filho), a dependência é uma interface no Domain, implementada por um adapter Infrastructure que **falha alto** (`NotImplementedException`) até o Sprint 5 substituir por persistência real — nunca um no-op silencioso que mascararia uso prematuro. A Etapa Automatizada é a exceção: sua chamada HTTP já roda de verdade, sem depender de Demanda nenhuma.

**`Configuracao` da etapa é polimórfica via `[JsonPolymorphic]`/`[JsonDerivedType]` (System.Text.Json, BCL — não fere a regra de Domain sem framework).** Evita um DTO achatado com um campo por tipo (6 tipos de configuração bem distintos). O corpo da requisição já chega tipado corretamente na Application layer a partir do discriminador `"tipo"` no JSON.

**Dependências de handler abstraídas atrás de interfaces do Domain** (`IResolvedorValorCampo`, `INotificadorEtapa`, `ICriadorSubprocesso`, `IVerificadorDesdobramentos`), implementadas por adapters "NaoImplementado" que lançam exceção clara — mesmo padrão de inversão de dependência do `IVerificadorEquipe`/`IVerificadorUsuario` (Sprint 3), aplicado aqui para fronteiras que literalmente ainda não têm dado real por trás.

## 3. Desvios do planejado

Nenhum desvio de escopo formal — o alcance real ficou exatamente como decidido na seção 2 (configuração + decisão testável, não execução completa), que já era a leitura mais defensável do roadmap dado que Sprint 5 é "Execução de Demandas" separadamente.

## 4. Bugs e resoluções

**Achado testando manualmente (curl + inspeção direta do Postgres, não só teste automatizado):** `GET /api/v1/etapas/{id}` de uma etapa Automatizada devolvia 500 "erro interno" em vez do detalhe esperado. O teste de integração automatizado só acusou uma `KeyNotFoundException` genérica ao ler `configuracao.url` — a causa raiz só ficou clara inspecionando o JSON bruto gravado no banco via `psql` direto. Raiz real: a coluna era `jsonb`, e **Postgres não preserva a ordem original das chaves de um objeto ao armazenar como jsonb** (reordena internamente no formato binário). A leitura polimórfica do `System.Text.Json` exige que o discriminador de tipo seja a **primeira** propriedade do objeto — mesmo com o discriminador presente, se não for o primeiro, lança `NotSupportedException`. Fix: a coluna `Configuracao` do `Etapa` passou a ser `json` (não `jsonb`) — grava o texto literal, sem reordenar. Como esse campo nunca é consultado via JSON path no SQL (sempre carregado e desserializado em C#), não custa a indexação binária do jsonb. `ConfiguracaoAcesso` (mesma entidade) continua `jsonb` normalmente — não é polimórfica, não sofre o mesmo problema.

**Achado em revisão manual (mesma classe recorrente RFC 9457 do CNPJ/Sprint 1, papel-Admin/Sprint 2, Lista-Opções/Sprint 3):** `CriarEtapaCommand`/`AtualizarEtapaCommand` não espelhavam a validação interna de cada `Configuracao` (URL válida, ramos não-vazios, status HTTP no intervalo válido etc.) — essas falhas caíam no `Results.Problem` ad-hoc em vez do formato com dict `errors` por campo. Fix: os validators reaproveitam `ConfiguracaoEtapa.Validar()` — o MESMO método que o Domain já chama — em vez de duplicar as 6 regras type-a-tipo. Zero lógica de negócio duplicada, formato RFC 9457 correto.

## 5. Código relevante

- `backend/src/Processa.Modules.Processos/Infrastructure/Configuracoes/EtapaConfiguration.cs` — o `HasColumnType("json")` e o comentário explicando por quê; único campo do projeto que não é jsonb.
- `backend/src/Processa.Modules.Processos/Domain/ConfiguracaoEtapa.cs` — os 6 records de configuração + os atributos de polimorfismo.
- `backend/src/Processa.Modules.Processos/Domain/Etapa*Handler.cs` (7 arquivos) — cada um vale uma leitura rápida pra ver o padrão de dependência abstraída.
- `backend/src/Processa.Modules.Processos/Infrastructure/ClienteHttpEtapa.cs` — o único adapter real (não stub) desta sprint.

## 6. Bloqueios e dependências

Nenhum bloqueio pro fechamento deste sprint. Dois riscos técnicos documentados conscientemente (não bloqueiam, mas precisam de atenção antes do Sprint 5 ligar a execução real) — ver seção "Risco técnico" do resumo geral e `.faf/pendencias.faf`.
