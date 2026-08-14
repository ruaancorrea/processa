# Sprint 2 — Resumo Geral

Sprint 2 do cronograma (`docs/07-roadmap/backlog-sprints.md`) · Board PROJ · Branch `feature/processa-sprint2-clientes-equipes`

## Por Epic/Feature

| Feature | Status | Bloqueios |
|---|---|---|
| PROJ-37 — CRUD de equipes e membros com papel (gestor/analista) | Pronto — critérios batidos | Nenhum |
| PROJ-38 — CRUD de clientes e grupos de clientes | Pronto | Nenhum |
| PROJ-39 — CRUD de contatos do cliente | Pronto | Nenhum |
| PROJ-40 — Vínculo responsável-cliente por equipe | Pronto | Nenhum |

## Por Tecnologia

**Novas dependências:** nenhuma — módulo Clientes reaproveita exatamente o mesmo conjunto de pacotes já usados por Identidade (EF Core, Npgsql, FluentValidation, MediatR).

**Mudanças relevantes de dependência:** nenhuma.

**Mudanças em contratos de API:**
- `POST/GET /api/v1/equipes`, `GET/PUT /api/v1/equipes/{id}`, `POST /api/v1/equipes/{id}/{desativar,reativar}` — novos
- `POST/DELETE /api/v1/equipes/{id}/membros[/{usuarioId}]` — novos
- `POST/GET /api/v1/grupos-clientes` — novos
- `POST/GET /api/v1/clientes`, `GET/PUT /api/v1/clientes/{id}`, `POST /api/v1/clientes/{id}/{suspender,inativar,reativar}` — novos
- `POST /api/v1/clientes/{id}/contatos`, `POST /api/v1/contatos-cliente/{id}/{desativar,reativar}` — novos
- `POST /api/v1/clientes/{id}/responsaveis`, `DELETE /api/v1/responsaveis-cliente/{id}` — novos

**Schema de banco (EF Core migrations):** 2 migrations novas — `AdicionarEquipes` (Identidade: tabelas `equipes`, `membros_equipe`, índice único `(equipe_id, usuario_id)`) e `CriarClientes` (1ª migration do módulo Clientes: `clientes` com índice único composto `(tenant_id, cnpj)`, `grupos_cliente`, `contatos_cliente`, `responsaveis_cliente` com índice único **parcial** `(tenant_id, cliente_id, usuario_id, equipe_id) WHERE removido_em IS NULL`).

## Métricas

| Métrica | Valor |
|---|---|
| Arquivos alterados | 101 |
| Linhas | +4.399 / -38 |
| Testes automatizados (backend) | 96 (fim do Sprint 1) → **172** (24 arquitetura + 131 unitários + 35 integração) |
| Cobertura de testes (backend) | 83,6% (Sprint 1) → **83,34%** linha (gate: 80%) |
| Vulnerabilidades (dotnet list --vulnerable) | 0 |
| QA manual (curl contra Postgres real) | Fluxo completo tenant→equipe→membro→cliente→contato→responsável, mais CNPJ inválido/duplicado e papel=Admin rejeitado — ver `qa/processa-api-sprint2.http` |
| Rodadas de revisão | 1 (5 skills: code-review, senior-backend, senior-security, senior-frontend, find-bugs) |
| Migrations novas | 2 (`AdicionarEquipes`, `CriarClientes`) |
| Bugs reais encontrados na revisão | 2 (mesma classe do achado de CNPJ do Sprint 1 — regra de negócio sem espelho no FluentValidation) |
| Bugs reais encontrados gerando a migration | 1 (índice composto com owned type não compõe via `OwnsOne`) |
| Regra de arquitetura nova | 1 (`Modulo_NaoDependeDeOutroModulo`, 6 casos — nenhum módulo referencia outro diretamente) |

## Risco Técnico

- **`ObterEquipePorIdQuery` tem uma consulta N+1** — o handler busca cada membro da equipe individualmente (`usuarioRepository.ObterPorIdAsync` por membro) pra montar o nome de exibição. Aceitável pro tamanho de equipe esperado (dezenas, não milhares, de membros), mas vale otimizar com um `ObterVariosPorIdAsync` em lote se equipes muito grandes aparecerem no piloto.
- **RBAC de clientes é global por perfil, não por equipe** — mesma limitação já aceita conscientemente no projeto de referência para o mesmo tipo de dado: um Gestor de qualquer equipe pode ler/escrever qualquer cliente do tenant, não só os da(s) equipe(s) responsável(is). Reavaliar se o piloto trouxer requisito de isolamento por equipe.
- **Comunicação entre módulos via interface em Shared.Kernel é um padrão novo, ainda usado uma vez só** (`IVerificadorMembroEquipe`) — funciona e está coberto por teste de arquitetura, mas vale observar se o padrão escala bem quando mais módulos precisarem se comunicar (Sprint 3+, quando Processos precisar consultar Clientes).

## Pendências

| Pendência | Motivo | Plano de ação |
|---|---|---|
| N+1 em `ObterEquipePorIdQuery` | Equipes pequenas no MVP, não é gargalo real ainda | Otimizar com fetch em lote se o piloto trouxer equipes grandes |
| RBAC de clientes sem isolamento por equipe | Mesma decisão consciente do projeto de referência | Reavaliar se exigência de isolamento aparecer antes do piloto (Sprint 12) |
