# Sprint 1 — Apresentação Técnica

Branch: `feature/processa-sprint1-identidade` · Issues: PROJ-16 (PROJ-33, PROJ-34, PROJ-35, PROJ-36) · PR #3 + hotfix PR #4

## 1. Entregas

| Feature/Fix | Status | Stack |
|---|---|---|
| Cadastro de tenant + onboarding do admin (PROJ-33) | OK | `CriarTenantCommand`, `Cnpj`/`Email` value objects com validação real |
| Autenticação JWT — access + refresh com rotação (PROJ-34) | OK | HMAC-SHA256, access 15min, refresh opaco 7 dias (hash SHA-256 em repouso) |
| RBAC por policy (Admin/Gestor/Analista) (PROJ-35) | OK | `AuthorizationBuilder` + `RequireRole`, nunca checagem manual de string |
| Bloqueio de conta após 5 falhas consecutivas (PROJ-36) | OK | Lógica pura no agregado `Usuario`, 15min de bloqueio |
| Middleware global de exceção (RFC 9457) | OK | `GlobalExceptionHandler : IExceptionHandler`, traduz `ValidationException` e `DbUpdateException` (unique violation) |
| Multi-tenancy via Global Query Filter | OK | `HasQueryFilter` em `Usuario`, `IgnoreQueryFilters()` explícito nos 2 fluxos que legitimamente cruzam tenant |
| Frontend: token em memória + cookie httpOnly | OK | `authStore.ts` (nunca localStorage), `credentials: "include"` em toda chamada |
| 63 testes unitários, 18 arquitetura, 15 integração | OK | xUnit, NSubstitute, Testcontainers.PostgreSql (Postgres real, não H2/sqlite) |

## 2. Decisões de arquitetura

- **E-mail de usuário único globalmente**, não escopado por tenant — os requisitos citavam "único no tenant", mas isso exige saber o tenant ANTES do login (subdomínio? seletor de organização?), não especificado e sem roteamento implementado. Login vira simplesmente email+senha.
- **Refresh token opaco em hash no Postgres**, sem reuse-detection via Redis — implementar o desenho completo do ADR-005 (Redis + detecção de reuso) ampliaria demais o escopo de um sprint que já inclui EF Core, JWT, RBAC e bloqueio de conta pela primeira vez. Suporta revogação explícita (logout) e expiração; reuse-detection automática fica como pendência registrada, não implementada pela metade silenciosamente.
- **Refresh token em cookie httpOnly+Secure+SameSite=Strict, access token só em memória no frontend** — resolve a pendência de segurança aberta desde a revisão do Sprint 0. `SameSite=Strict` + `Path=/api/v1/auth` já mitiga CSRF sem exigir token CSRF separado.
- **Mensagem de conta bloqueada revela existência do e-mail** (aceito conscientemente) — o handler de login usa mensagem genérica idêntica para "não existe" e "senha errada" especificamente para não ajudar enumeração, mas a mensagem de bloqueio é distinta (só contas existentes bloqueiam). Trade-off de UX comum na indústria: usuário legítimo bloqueado precisa saber por quanto tempo. Risco residual mitigado pelo próprio rate-limit implícito do bloqueio.
- **JWT Bearer com `MapInboundClaims = false`** — sem isso, o `JwtSecurityTokenHandler` renomeia claims curtas conhecidas ("sub" → `ClaimTypes.NameIdentifier`) na validação, quebrando qualquer `FindFirst(JwtRegisteredClaimNames.Sub)` no código de aplicação.

## 3. Desvios do planejado

- **5 bugs reais encontrados testando, antes de qualquer commit** (não é achado de revisão — foi rodando os testes de integração de verdade contra Postgres real via Testcontainers): divergência de chave JWT entre emissão/validação em cenário de teste (causa raiz: `Program.cs` lia `builder.Configuration` diretamente, cedo demais pro override de configuração do `WebApplicationFactory` chegar; corrigido unificando em `IOptions<JwtOptions>` — a mesma fonte que o `JwtTokenGenerator` já usava); contrato 422 do CNPJ inválido sem o dict `errors`; cookie `Secure=true` "sumindo" nos testes (CookieContainer recusa Secure sobre `http://`, corrigido forçando `https://` no client de teste — TestServer é in-memory, não exige certificado real); `IDomainEvent` acoplado a `MediatR.INotification`, violando a própria regra de arquitetura que o projeto testa automaticamente; claim "sub" não resolvida em `/me`.
- **4 achados reais na revisão de 5 skills**, todos com fix aplicado antes do PR: contador de bloqueio de conta nunca resetava após a janela expirar (uma falha isolada pós-expiração re-bloqueava na hora — bug real de lógica de negócio, não só teste); timing side-channel no login (bcrypt pulado quando usuário não existe, permitindo enumerar e-mails por latência da resposta); corrida entre checagem de unicidade e commit no cadastro de tenant vazando como 500 em vez de 422; `ObterPorIdAsync` que ignora o filtro de tenant sem deixar isso óbvio no nome (renomeado pra `ObterPorIdIgnorandoTenantAsync`).
- **CVE publicada no mesmo dia do merge** — o PR #3 quebrou o CI de `develop` porque `SSH.NET 2024.1.0` (transitiva via `Testcontainers.PostgreSql`, usada só em testes) recebeu um advisory de severidade alta (`GHSA-q939-rpr3-3284`, path traversal em `ScpClient.Download`) publicado horas antes. Mesmo a versão mais recente do Testcontainers ainda trazia `SSH.NET` dentro da faixa vulnerável (`<= 2025.1.0`); resolvido com referência direta a `SSH.NET 2026.0.0` (primeira versão corrigida) pra forçar a resolução transitiva — hotfix em PR separado (#4).
- **Jira não foi atualizado durante o trabalho** — PROJ-16 e as 4 subtasks continuaram "A fazer" no board mesmo com o código pronto e mergeado, porque a sessão retomou de um contexto comprimido direto na depuração de testes, sem reabrir o Jira antes. Corrigido a posteriori: subtasks e task-pai movidas pra "Feito", comentário com os links dos PRs.

## 4. Bugs e resoluções

**Divergência de chave JWT entre emissão e validação** — `Program.cs` fazia `builder.Configuration.GetSection("Jwt")["SecretKey"]` diretamente, antes de `builder.Build()`; sob `WebApplicationFactory`, o override de configuração do fixture de teste (`ConfigureAppConfiguration`) não chegava a tempo dessa leitura antecipada, enquanto o `JwtTokenGenerator` (via `IOptions<JwtOptions>`, resolvido preguiçosamente em request-time) sempre via a configuração final — resultado: token assinado com uma chave, validado contra outra (`WWW-Authenticate: invalid_token, signature key was not found`).
Fix: `AddJwtBearer()` configurado via `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtOptions>>(...)`, mesma fonte única de verdade que o gerador de token.

**Contrato 422 do CNPJ inválido sem o dict `errors` documentado** — a validação de CNPJ (dígito verificador) vivia só no value object (`Cnpj.Criar`, retorna `Result`), nunca passava pelo `ValidationBehavior`/`FluentValidation`, então o endpoint devolvia um `Results.Problem` manual sem `errors`, divergindo do contrato RFC 9457 documentado.
Fix: `Cnpj.EhValido(string?)` exposto como helper estático (reaproveitando a regra de dígito verificador, sem duplicar) + `.Must(Cnpj.EhValido)` no `CriarTenantCommandValidator`.

**Contador de tentativas de login nunca resetava após o bloqueio expirar** — `Usuario.RegistrarTentativaFalha()` só zerava `TentativasLoginFalhas` em `RegistrarLoginSucesso()`; passados os 15 minutos do bloqueio, uma única senha errada voltava a incrementar pra 6 (ainda `>= 5`), re-bloqueando na hora — o usuário nunca tinha as 5 tentativas frescas que a política documentada promete.
Fix: reset do contador no início de `RegistrarTentativaFalha()` quando `BloqueadoAte` já passou.

**Timing side-channel habilitando enumeração de e-mail no login** — quando o e-mail não existe, o handler retornava cedo sem nunca chamar `passwordHasher.Verificar`; quando existe mas a senha está errada, o bcrypt (cost 12, ~100-300ms) sempre roda. A diferença de latência é mensurável e permite inferir quais e-mails estão cadastrados.
Fix: hash bcrypt fictício fixo, verificado mesmo quando o usuário não é encontrado, equalizando o tempo de resposta.

**Corrida de unicidade no cadastro de tenant virando 500** — duas requisições concorrentes podem passar pela checagem de unicidade da Application layer (`ExisteCnpjAsync`/`ExisteEmailAsync`) antes de qualquer uma commitar; a constraint `UNIQUE` do Postgres barra a segunda no `INSERT`, mas nada traduzia esse `DbUpdateException` pro contrato 422 esperado.
Fix: `GlobalExceptionHandler` reconhece `DbUpdateException { InnerException: PostgresException { SqlState: UniqueViolation } }` e devolve 422 "registro em conflito". Coberto por teste de integração disparando duas requisições reais com `Task.WhenAll`.

## 5. Código relevante

- `backend/src/Processa.Api/Program.cs` — `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtOptions>>(...)` como fonte única de verdade pra validação de JWT, evitando a classe inteira de bug de "duas leituras de configuração divergentes".
- `backend/src/Processa.Api/GlobalExceptionHandler.cs` — agora trata 2 tipos de exceção (`ValidationException` e `DbUpdateException`/unique violation), ambos convertidos pro mesmo contrato RFC 9457.
- `backend/src/Processa.Modules.Identidade/Domain/Usuario.cs::RegistrarTentativaFalha()` — reset de contador na expiração da janela de bloqueio; regressão coberta com teste que usa reflection pra simular a passagem do tempo (sem abstração de relógio no projeto ainda — candidato a `TimeProvider` se aparecer uma 2ª necessidade de controlar tempo em teste).
- `backend/tests/Processa.IntegrationTests/Identidade/IdentidadeApiFixture.cs` — fixture Testcontainers com `CreateClient` forçando `https://localhost` como BaseAddress (via `new` hiding, não `override` — `WebApplicationFactory.CreateClient(options)` não é virtual) pra destravar cookies `Secure=true` no `CookieContainer`.

## 6. Bloqueios e dependências

Nenhum bloqueio externo. Pendências abertas (não bloqueantes, ver `resumo-geral.md`): rate limiting global por IP no login, reuse-detection de refresh token via Redis, isolamento de tenant mais forte que Global Query Filter.
