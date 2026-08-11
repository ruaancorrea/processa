# ADR-001 — Clean Architecture + Monólito Modular por Bounded Context

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Processa precisa de limites de domínio claros (identidade, clientes, processos, documentos, notificações, portal) sem o custo operacional de microsserviços logo no MVP — deploy distribuído, consistência eventual entre serviços, observabilidade multi-processo — quando a equipe ainda é pequena e o produto ainda está validando mercado.

## Decisão

Backend em **monólito modular**: um único processo/deploy, dividido em módulos por bounded context (`Processa.Modules.Identidade`, `Modules.Clientes`, `Modules.Processos`, `Modules.Documentos`, `Modules.Notificacoes`, `Modules.Portal`), cada um internamente estruturado em Clean Architecture (`Domain → Application → Infrastructure/Presentation`).

Regras de dependência:
1. `Domain` não referencia nenhum framework nem outra camada.
2. `Application` depende apenas de `Domain` (usa MediatR para casos de uso como Commands/Queries).
3. `Infrastructure` e `Presentation` dependem de `Application`, nunca o contrário.
4. Um módulo nunca referencia diretamente a `Infrastructure` ou `Domain` de outro módulo — comunicação entre módulos ocorre via eventos de domínio in-process (MediatR notifications) ou contratos expostos pela `Application` layer.

Essas regras são validadas automaticamente por testes de arquitetura (`Processa.ArchitectureTests`, usando `NetArchTest.Rules`) que rodam no CI — uma violação quebra o build, não depende de review manual.

## Alternativas consideradas

- **Microsserviços desde o início:** rejeitado — custo de infraestrutura (service mesh, tracing distribuído, deploy orquestrado) desproporcional ao estágio do produto; extração de um módulo para serviço próprio permanece possível depois, exatamente porque os limites já existem no código.
- **Monólito não-modular (camadas horizontais únicas: `Controllers/`, `Services/`, `Repositories/`):** rejeitado — em poucos meses todo domínio vira uma bola de lama, sem fronteira que impeça um `ClienteService` de chamar direto o `EtapaRepository`.

## Consequências

- Deploy único simplifica CI/CD no MVP.
- Extração futura de um módulo (ex: `Modules.Notificacoes`, candidato natural por volume de I/O) para serviço próprio é possível sem reescrever lógica de domínio — só a camada de `Infrastructure`/transporte muda.
- Exige disciplina: testes de arquitetura são obrigatórios no CI desde o Sprint 0, não "adicionados depois".
