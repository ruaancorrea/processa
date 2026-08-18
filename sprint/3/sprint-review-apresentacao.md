# Sprint 3 — Apresentação

Sprint 3 do cronograma (`docs/07-roadmap/backlog-sprints.md`) — Motor de Processos I: Configuração · Board PROJ (PROJ-18, subtasks PROJ-41/42/43/44) · Branch `feature/processa-sprint3-motor-processos-configuracao`

## 1. Entregas

| Feature | Status | Stack |
|---|---|---|
| CRUD de tipos de processo (modos fixo/dinâmico/manual) | Pronto | Domain (`TipoProcesso`), Application (MediatR), EF Core/Postgres |
| Campos personalizados (8 tipos, opções restritas ao tipo Lista) | Pronto | `CampoPersonalizado`, coluna `opcoes` JSONB |
| CRUD de fluxos com fluxo padrão único por tipo de processo | Pronto | `Fluxo` + índice único parcial `(tipo_processo_id) WHERE fluxo_padrao = true` |
| Permissões de início por perfil/usuário | Pronto (configurável — enforcement é Sprint 5) | VO `PermissoesInicio`, coluna `permissoes_inicio` JSONB |

## 2. Decisões de arquitetura

**`Perfil` promovido para `Processa.Shared.Kernel`.** Mesma motivação do `Cnpj`/`Email` no Sprint 2: `TipoProcesso.PermissoesInicio` precisa referenciar `Perfil`, que vivia em `Identidade.Domain` — inacessível a outro módulo sem violar a regra de independência entre módulos. Terceira repetição desse padrão; documentado em `.claude/architecture.md` e `.faf/decisions.faf` como convenção, não decisão pontual.

**`PermissoesInicio.Criar` deixou de retornar `Result<PermissoesInicio>`.** Na primeira versão, seguia o padrão geral de factory de VO com `Result<T>`, mas a validação (dedupe de perfis, filtro de `Guid.Empty`) nunca produz uma falha real — não existe caminho de erro. Manter `Result<T>` ali era abstração sem uso, contra o princípio do projeto de não introduzir tratamento de erro para cenário que não acontece. Simplificado para uma factory direta.

**`PermissoesInicio` e `CampoPersonalizado.Opcoes` mapeados via `HasConversion` (coluna JSONB), não `OwnsOne().ToJson()`.** `PermissoesInicio` é um VO com construtor privado e sem setters — EF Core não consegue materializá-lo via a API nativa de owned-type-como-JSON sem reflection mais invasiva. Serialização manual via `System.Text.Json` no `HasConversion`, com um DTO interno só para (de)serialização, mantém o VO imutável e a Infrastructure isolada dessa decisão. Para `Opcoes` (lista de strings simples), foi necessário configurar um `ValueComparer` explícito — sem ele, o EF avisa (`Model.Validation 10620`) que não consegue detectar corretamente mudança de conteúdo vs. troca de referência.

**Índice único parcial como backstop real da invariante "um fluxo padrão por tipo de processo".** A Application layer garante a invariante na escrita (desmarcar o antigo antes de marcar o novo), mas o índice `(tipo_processo_id) WHERE fluxo_padrao = true` é quem realmente impede a violação em caso de bug ou concorrência — e foi ele quem pegou o bug descrito na seção 4.

## 3. Desvios do planejado

Nenhum desvio de escopo. O ambiente local precisou de dois ajustes não relacionados ao código: Docker Desktop precisou ser iniciado manualmente (não estava rodando no início da sessão) e `.env` tinha `POSTGRES_PORT=5433` desalinhado do `appsettings.Development.json` (que aponta para `5432`) — provavelmente resquício de uma sessão anterior em que a porta padrão estava ocupada. Realinhado para `5432` já que nada mais usa essa porta hoje.

## 4. Bugs e resoluções

**Achado testando (integração):** troca de fluxo padrão (`DefinirFluxoPadraoCommand`) violava intermitentemente o índice único parcial de `fluxos`, retornando 422 em vez de 204. Raiz: o handler desmarca o fluxo antigo e marca o novo, depois chama `SaveChangesAsync()` uma única vez — mas o EF Core não garante que o `UPDATE` de desmarcar rode antes do de marcar quando as duas entidades não têm relação de FK entre si (nesse caso, a entidade sendo marcada como padrão foi rastreada pelo `ChangeTracker` *antes* da que seria desmarcada, pela ordem de carregamento no handler). Resultado: por um instante dentro da mesma transação, duas linhas tinham `fluxo_padrao = true`, violando o índice. Fix: duas gravações — desmarcar e commitar, só depois marcar e commitar. Estado intermediário "zero fluxo padrão" é aceitável; "dois fluxos padrão" não é. Aplicado também em `CriarFluxoCommand` (mesmo risco ao promover um segundo fluxo a padrão na criação), mesmo esse teste específico não tendo pegado a falha — a causa raiz é idêntica e não dá pra confiar na ordem "por sorte" do EF.

**Achado em revisão manual (find-bugs):** `CriarCampoPersonalizadoCommand` não tinha regra de FluentValidation espelhando `CampoPersonalizado.Criar` ("tipo Lista exige ao menos uma opção" / "opções só fazem sentido pro tipo Lista") — mesma classe de achado recorrente do CNPJ (Sprint 1) e papel=Admin/contato sem meio de contato (Sprint 2): a falha caía no `Results.Problem` ad-hoc em vez do formato RFC 9457 com dict `errors` por campo. Fix: duas regras `RuleFor(x => x).Must(...).WithName("Opcoes")`, espelhando as duas direções da regra de domínio. `AtualizarCampoPersonalizadoCommand` **não** consegue o mesmo espelhamento — `Tipo` é imutável após a criação e não faz parte do corpo da requisição de update, e validators deste projeto são síncronos/sem acesso a repositório por convenção. Aceito conscientemente e documentado em `.faf/pendencias.faf`.

**Achado em revisão manual (find-bugs):** `DefinirPermissoesInicioCommand` tinha `List<Perfil> Perfis, List<Guid> UsuarioIds` não-nuláveis e chamava `.Distinct()` direto — um cliente mandando `null` (em vez de `[]`) pra "limpar permissões" gerava `NullReferenceException` não tratada (500 genérico via `GlobalExceptionHandler`, não um 422 claro). Fix: parâmetros viraram nuláveis com `?? []`, mesmo padrão defensivo já usado em `CampoPersonalizado.Criar`.

## 5. Código relevante

- `backend/src/Processa.Modules.Processos/Application/Fluxos/{CriarFluxoCommand,DefinirFluxoPadraoCommand}.cs` — a lógica de duas gravações; vale ler o comentário inline explicando por quê.
- `backend/src/Processa.Modules.Processos/Infrastructure/Configuracoes/FluxoConfiguration.cs` — índice único parcial + índice de suporte `(TenantId, TipoProcessoId)`.
- `backend/src/Processa.Modules.Processos/Infrastructure/Configuracoes/TipoProcessoConfiguration.cs` — `HasConversion` de `PermissoesInicio` pra JSONB.
- `backend/src/Processa.Modules.Processos/Domain/PermissoesInicio.cs` — VO sem `Result<T>` no factory.

## 6. Bloqueios e dependências

Nenhum bloqueio real pendente no fechamento deste sprint. `PermissoesInicio` fica sem efeito prático até o Sprint 5 (execução de demandas) — não é um bloqueio, é sequenciamento esperado do roadmap.
