# Sprint 3 — Resumo Geral

Sprint 3 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint3-motor-processos-configuracao`

## Por Epic/Feature

| Feature | Status (Jira) | Bloqueios |
|---|---|---|
| PROJ-41 — CRUD de tipos de processo (modos fixo/dinâmico/manual) | Feito | Nenhum |
| PROJ-42 — Campos personalizados (8 tipos) por tipo de processo | Feito | Nenhum |
| PROJ-43 — CRUD de fluxos com fluxo padrão único por tipo de processo | Feito | Nenhum |
| PROJ-44 — Permissões de início por perfil/usuário | Feito | Nenhum (enforcement real fica pro Sprint 5) |

## Por Tecnologia

**Novas dependências:** nenhuma — módulo Processos passou a usar os mesmos pacotes já presentes em Identidade/Clientes (`Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `FluentValidation.DependencyInjectionExtensions`), agora referenciados no `.csproj` (antes só tinha `MediatR` + `Shared.Kernel`).

**Mudanças relevantes de dependência:** nenhuma.

**Mudanças em contratos de API:**
- `POST/GET /api/v1/tipos-processo`, `GET/PUT /api/v1/tipos-processo/{id}`, `POST /api/v1/tipos-processo/{id}/{ativar,desativar}` — novos
- `PUT /api/v1/tipos-processo/{id}/permissoes-inicio` — novo
- `POST /api/v1/tipos-processo/{id}/campos`, `PUT/DELETE /api/v1/campos-personalizados/{id}` — novos
- `POST /api/v1/tipos-processo/{id}/fluxos`, `PUT /api/v1/fluxos/{id}`, `POST /api/v1/fluxos/{id}/definir-padrao`, `DELETE /api/v1/fluxos/{id}` — novos

**Schema de banco (EF Core migrations):** 1 migration nova — `CriarProcessos`, 1ª migration do módulo Processos: schema `processos` com tabelas `tipos_processo` (coluna `permissoes_inicio` JSONB), `campos_personalizados` (coluna `opcoes` JSONB), `fluxos` (índice único parcial `(tipo_processo_id) WHERE fluxo_padrao = true`).

## Métricas

| Métrica | Valor |
|---|---|
| Arquivos alterados | 65 |
| Linhas | +3.423 / -3 (working tree; sprint ainda não commitado no momento da medição) |
| Testes automatizados (backend) | 190 (fim do Sprint 2: 24 arquitetura + 131 unitários + 35 integração) → **272** (24 arquitetura + 196 unitários + 52 integração) |
| Cobertura de testes (backend) | 83,34% (Sprint 2) → **83,3%** linha (gate: 80%) |
| Vulnerabilidades (`dotnet list package --vulnerable`) | 0 |
| Snyk (`snyk test --all-projects` + `snyk code test`) | SCA: 0 vulneráveis (11 projetos, 246 dependências). SAST: 4 abertos, todos os mesmos falsos-positivos de "hardcoded credential" já documentados desde o Sprint 2 (dados de teste, não segredo real) — nenhum achado novo introduzido neste sprint |
| QA manual (curl contra Postgres real) | Fluxo completo equipe→tipo de processo (modo Fixo)→campo personalizado (Lista)→2 fluxos (troca de padrão)→permissões de início, mais o caso de borda "remover fluxo padrão havendo outro" — ver `qa/processa-api-sprint3.http` |
| Rodadas de revisão | 1 (revisão manual do diff completo — as 5 skills nomeadas no checklist retornam boilerplate genérico de Sentry/Django, achado já registrado em sessões anteriores; revisão feita lendo o diff linha a linha aplicando os mesmos critérios) |
| Migrations novas | 1 (`CriarProcessos`) |
| Bugs reais encontrados testando | 1 (violação intermitente do índice único parcial na troca de fluxo padrão) |
| Bugs reais encontrados em revisão manual | 2 (FluentValidation sem espelho pra regra Lista/Opções; `NullReferenceException` em `DefinirPermissoesInicioCommand` com listas nulas) |
| Índice de suporte adicionado após revisão | 1 (`fluxos (TenantId, TipoProcessoId)` — índice parcial único não cobre a consulta "listar todos os fluxos de um tipo") |

## Risco técnico

- **`PermissoesInicio` é dado configurado, mas sem efeito prático ainda.** Não existe endpoint de criação de Demanda (Sprint 5) que consulte essa permissão. Risco baixo — é sequenciamento esperado do roadmap, não uma lacuna de segurança (a rota de configuração já está corretamente protegida por RBAC `GestorOuAdmin`).
- **`AtualizarCampoPersonalizadoCommand` não consegue espelhar a regra "Lista exige opções" em FluentValidation** (ver `.faf/pendencias.faf`) — o erro de domínio ainda existe e bloqueia a operação (422), só não tem o dict `errors` por campo no formato RFC 9457 completo. Simplificação consciente: introduzir validators assíncronos com acesso a repositório seria um padrão novo no projeto, não vale a pena só por esse caso isolado.
- **Token OAuth do Snyk expira entre sessões** — precisou de reautenticação (2 tentativas de `snyk auth` deram timeout esperando login no navegador, 3ª funcionou) antes de rodar o scan deste sprint. Não é uma regressão nem bloqueio real, mas confirma que esse passo do checklist sempre vai exigir presença ativa pra completar o OAuth a tempo.

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| Enforcement de `PermissoesInicio` ao criar Demanda | Endpoint de Demanda ainda não existe | Implementar a checagem no handler de criação de Demanda quando o Sprint 5 começar |
| `AtualizarCampoPersonalizadoCommand` sem dict `errors` pro caso Lista/Opções | Regra depende do `Tipo` atual da entidade, que não está no corpo da requisição de update, e validators do projeto são síncronos | Reavaliar só se validators assíncronos virarem padrão aceito no projeto por outro motivo |
| N+1 em `ObterEquipePorIdQuery` (Sprint 2, ainda aberta) | Equipes pequenas no MVP | Otimizar com fetch em lote se o piloto trouxer equipes grandes |
| RBAC de clientes/tipos de processo sem isolamento por equipe (Sprint 2, ainda aberta) | Decisão consciente, mesma do projeto de referência | Reavaliar se exigência de isolamento aparecer antes do piloto |
