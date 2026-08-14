# Sprint 2 — Apresentação Técnica

Branch: `feature/processa-sprint2-clientes-equipes` · Issues: PROJ-17 (PROJ-37, PROJ-38, PROJ-39, PROJ-40)

## 1. Entregas

| Feature/Fix | Status | Stack |
|---|---|---|
| CRUD de equipes e membros com papel (PROJ-37) | OK | `Equipe`/`MembroEquipe` em `Processa.Modules.Identidade`, papel restrito a Gestor/Analista |
| CRUD de clientes e grupos de clientes (PROJ-38) | OK | Novo módulo `Processa.Modules.Clientes`; CNPJ único **por tenant** (índice composto) |
| CRUD de contatos do cliente (PROJ-39) | OK | Pelo menos um meio de contato (e-mail/telefone/celular) obrigatório |
| Vínculo responsável-cliente por equipe (PROJ-40) | OK | Soft-delete com índice único **parcial**, reativável sem duplicar linha |
| 131 testes unitários, 24 arquitetura, 35 integração | OK | xUnit, NSubstitute, Testcontainers.PostgreSql |

## 2. Decisões de arquitetura

- **Equipe/MembroEquipe entram no módulo Identidade**, não num módulo "Equipes" separado — `.claude/architecture.md` só reservava um módulo "Clientes" pro Sprint 2 (escopo "Clientes, contatos, grupos"), sem módulo dedicado a equipes. Equipe é fundamentalmente sobre organizar `Usuario` (que Identidade já possui), e o projeto de referência usado como inspiração modela hierarquia de equipes no mesmo contexto de identidade — mesmo precedente seguido aqui.
- **Cnpj e Email movidos de `Identidade.Domain` para `Shared.Kernel`** — `Cliente` (novo, módulo Clientes) precisa da mesma validação de CNPJ (dígito verificador mod-11) que `Tenant` já usava, e `ContatoCliente` precisa da mesma validação de e-mail que `Usuario`. Um módulo não pode referenciar o Domain de outro diretamente; duplicar o algoritmo de checksum entre dois módulos seria pior que promover o VO pro Shared Kernel — é exatamente o caso de uso que um shared kernel resolve bem (conceito genuinamente cross-cutting, não acidentalmente compartilhado).
- **Comunicação entre módulos via interface em Shared.Kernel, não `ISender` cross-module** — `AdicionarResponsavelCommand` (Clientes) precisa saber se um usuário é membro de uma equipe (informação que só Identidade tem). A primeira tentativa enviava a query `ObterEquipePorIdQuery` de Identidade via `ISender`, mas isso exigia referenciar o tipo em compile-time, o que exigiria um `ProjectReference` de `Clientes.csproj` pra `Identidade.csproj` — acoplamento de compilação entre módulos que deveriam poder evoluir/ser extraídos independentemente. Resolvido com `IVerificadorMembroEquipe` definida em `Shared.Kernel`, implementada em `Identidade.Infrastructure`, registrada no DI de Identidade — inversão de dependência exatamente na fronteira do módulo. Nova regra de arquitetura (`Modulo_NaoDependeDeOutroModulo`, 6 casos) adicionada aos testes automatizados pra essa disciplina nunca regredir silenciosamente.
- **`Cnpj` em `Cliente` mapeado via `HasConversion`, não `OwnsOne`** — achado real gerando a migration: um índice único composto `(tenant_id, cnpj)` não compõe diretamente num `HasIndex` lambda-based quando a segunda coluna vem de um owned-type navigation property; `HasConversion` trata a propriedade como escalar convertido, permitindo o índice composto normal. `Tenant.Cnpj` (Sprint 1) continua com `OwnsOne`, porque seu índice é simples (só `Cnpj.Numero`, sem compor com outra coluna) — os dois padrões coexistem conforme a necessidade real de cada caso.
- **RBAC: mutações estruturais de equipe exigem Admin; operações de cliente aceitam Gestor ou Admin** — equipe é estrutura organizacional do tenant inteiro (mesmo nível de "quem pode fazer o quê" do Sprint 1); cliente é operação do dia a dia, mais compatível com o nível de acesso de um Gestor. Nenhuma escrita é liberada pra Analista neste sprint — consistente com o papel de execução (não estrutural) que Analista tem nos requisitos.
- **RBAC de clientes é global por perfil, não por equipe** — qualquer Gestor/Admin do tenant pode gerenciar qualquer cliente, independente de pertencer à equipe responsável. Mesma decisão consciente documentada no projeto de referência para o mesmo tipo de dado — sem isolamento por equipe neste sprint; reavaliar se a Sprint 3+ trouxer requisito explícito.

## 3. Desvios do planejado

- **2 bugs reais achados na revisão, mesma classe do CNPJ do Sprint 1**: a regra "papel de membro de equipe não pode ser Admin" e a regra "contato precisa de pelo menos um meio de contato" viviam só no Domain (`Result.Failure`), nunca passavam pelo `FluentValidation`/`ValidationException`, então a resposta 422 não trazia o dict `errors` que o contrato RFC 9457 documentado promete — confirmado empiricamente via curl antes de corrigir, não só por inspeção de código. Fix: regras espelhadas no validator do comando (`.NotEqual(Perfil.Admin)`, `.Must()` de campo cruzado), reaproveitando a mensagem do Domain sem duplicar a lógica de decisão. Padrão a vigiar em sprints futuros: toda regra de negócio que pode falhar num `Criar`/`Adicionar` de comando precisa ter um espelho no FluentValidation, não só no Domain.
- **Rename da fixture de teste de integração**: `IdentidadeApiFixture` virou `ProcessaApiFixture` — testes de Clientes dependem de dado criado por Identidade (uma `AdicionarResponsavelCommand` precisa de uma `Equipe` real), então precisam do mesmo processo/mesmo container Postgres. Migrou a migração de ambos os `DbContext` (Identidade e Clientes) pra dentro da mesma fixture compartilhada, coleção `"ProcessaApi"` em vez de `"Identidade"`.

## 4. Bugs e resoluções

**Papel=Admin em membro de equipe sem `errors` dict** — `MembroEquipe.Adicionar()` recusa `Perfil.Admin` corretamente (regra de negócio real, verificada por teste de domínio), mas essa recusa só existia como `Result.Failure` no Domain; o endpoint devolvia `Results.Problem(detail: ...)` sem o dict de campo. Fix: `AdicionarMembroCommandValidator` ganhou `RuleFor(x => x.Papel).NotEqual(Perfil.Admin)`, então a falha agora passa pelo `ValidationBehavior`/`GlobalExceptionHandler` como qualquer outro erro de campo.

**Contato sem meio de contato sem `errors` dict** — mesma causa raiz, em `ContatoCliente.Criar()`. Fix: `AdicionarContatoCommandValidator` ganhou uma regra de classe (`RuleFor(x => x)...Must(...)`), já que é uma regra de campo cruzado (e-mail OU telefone OU celular), não de um campo isolado.

**Índice composto `(tenant_id, cnpj)` não gerava com `OwnsOne`** — achado durante `dotnet ef migrations add`, não em revisão: `HasIndex("TenantId", "Cnpj.Numero")` (string-based, caminho de navegação) lançava `Unable to create a 'DbContext'... no property type was specified`. Fix: `Cnpj` em `Cliente` mapeado via `HasConversion` (`cnpj => cnpj.Numero`, `numero => Cnpj.Criar(numero).Value`), permitindo `HasIndex(c => new { c.TenantId, c.Cnpj })` normal — e ajustado `ClienteRepository.ExisteCnpjAsync` pra comparar o VO inteiro (`c.Cnpj == cnpj`), já que `c.Cnpj.Numero` não traduziria mais pra SQL depois da conversão.

## 5. Código relevante

- `backend/src/Processa.Shared.Kernel/IVerificadorMembroEquipe.cs` — contrato de fronteira entre módulos; candidato a padrão a repetir sempre que um módulo precisar de uma pergunta pontual respondida por outro.
- `backend/tests/Processa.ArchitectureTests/CleanArchitectureTests.cs::Modulo_NaoDependeDeOutroModulo` — nova regra, matriz de 6 módulos × não-dependência dos outros 5; pega qualquer `ProjectReference` cruzado que reapareça por conveniência no futuro.
- `backend/src/Processa.Modules.Clientes/Infrastructure/Configuracoes/ClienteConfiguration.cs` — `HasConversion` em vez de `OwnsOne` pro Cnpj, com o comentário explicando por quê (evita alguém "corrigir" de volta pro padrão do Sprint 1 sem saber da limitação).

## 6. Bloqueios e dependências

Nenhum bloqueio externo.
