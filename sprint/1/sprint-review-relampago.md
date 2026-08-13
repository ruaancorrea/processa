SPRINT 1 — IDENTIDADE, TENANTS E CONTROLE DE ACESSO (PROJ-16) — 2026-08-11/13

ENTREGAS:
• Cadastro de tenant (escritório) + onboarding do usuário admin (PROJ-33) → OK
• Autenticação JWT — access token 15min + refresh token opaco 7 dias, rotação a cada refresh (PROJ-34) → OK
• RBAC por policy (Admin/Gestor/Analista) em endpoints reais e de exemplo (PROJ-35) → OK
• Bloqueio de conta após 5 tentativas de login falhas consecutivas (PROJ-36) → OK
• Middleware global de exceção (RFC 9457 Problem Details) → OK
• Value objects com validação real: Cnpj (dígito verificador mod-11), Email → OK
• Frontend: token em memória (não localStorage), `credentials: include` → OK
• 63 testes unitários, 18 de arquitetura, 15 de integração (Postgres real via Testcontainers) → OK

DECISÕES:
• E-mail de usuário único GLOBALMENTE, não por tenant → tenant só é conhecido DEPOIS do login; sem roteamento por subdomínio implementado
• Refresh token opaco em hash no Postgres (não Redis com reuse-detection) → escopo completo do ADR-005 ampliaria demais o Sprint 1; reuse-detection fica como pendência
• Token: refresh em cookie httpOnly+Secure+SameSite=Strict, access só em memória no frontend → resolve pendência de segurança aberta desde o Sprint 0
• Mensagem de "conta bloqueada" revela existência do e-mail (aceito conscientemente) → trade-off de UX comum na indústria, risco residual mitigado pelo próprio rate-limit implícito do bloqueio

BUGS (achados em teste de integração/verificação manual, antes de qualquer commit):
• Chave JWT divergia entre emissão e validação em teste → Raiz: Program.cs lia config direto, cedo demais pro override do WebApplicationFactory → Fix: unificado em IOptions<JwtOptions>
• Contrato 422 do CNPJ inválido não trazia o dict `errors` documentado → Raiz: validação só no value object (Result), nunca passava pelo FluentValidation/ValidationException → Fix: `Cnpj.EhValido()` + regra no validator do comando
• Cookie Secure=true "sumia" nos testes → Raiz: CookieContainer do HttpClient recusa Secure sobre http://, BaseAddress padrão do WebApplicationFactory é http → Fix: fixture força https:// (só destrava o CookieContainer, TestServer é in-memory)
• `IDomainEvent : INotification` (MediatR) → violava a própria regra de arquitetura testada (Domain sem framework) → Fix: interface desacoplada, virou marcador puro
• Claim "sub" não resolvida em `/me` → Raiz: JwtSecurityTokenHandler remapeia "sub"→ClaimTypes.NameIdentifier por padrão → Fix: `MapInboundClaims = false`

BUGS (achados na revisão de 5 skills — code-review, senior-backend, senior-security, senior-frontend, find-bugs):
• Contador de tentativas de login nunca reiniciava após a janela de bloqueio expirar → uma falha isolada pós-expiração re-bloqueava na hora → Fix: reset do contador quando `BloqueadoAte` já passou
• Timing side-channel no login (bcrypt pulado quando usuário não existe) → permitia enumerar e-mails cadastrados por latência → Fix: hash fictício rodado mesmo quando usuário não é encontrado
• Corrida entre checagem de unicidade e commit no cadastro de tenant → vazava como 500 em vez de 422 "já existe" → Fix: `GlobalExceptionHandler` trata `DbUpdateException`/unique violation
• `ObterPorIdAsync` ignorava filtro de tenant sem deixar isso óbvio → risco de uso indevido por código futuro → Fix: renomeado pra `ObterPorIdIgnorandoTenantAsync`

BUGS (pós-merge, achado pelo CI, não pela revisão local):
• SSH.NET 2024.1.0 (transitiva via Testcontainers) — CVE publicada no MESMO DIA do merge (GHSA-q939-rpr3-3284, path traversal em ScpClient.Download) → Fix: referência direta a SSH.NET 2026.0.0 (PR #4, hotfix)

BLOQUEIOS:
• Nenhum bloqueio externo

MÉTRICAS:
• PR #3: 65 arquivos (+2.889/-17) | PR #4 (hotfix): 2 arquivos (+3/-3)
• Testes: 39→96 backend (18 arquitetura + 63 unitários + 15 integração) | 7→8 frontend
• Cobertura: 84,21%→83,6% linha (gate: 80%, cai um pouco por causa do volume de Infrastructure/Presentation glue novo, ainda dentro do gate)
• Rodadas de revisão: 1 (5 skills) + verificação manual ponta a ponta completa (curl contra Postgres real)
• Releases: v1.0.0-dev.2, v1.0.0-dev.3

NEXT:
• Abrir PR de feature/processa-sprint1-identidade → develop → feito, mergeado (PR #3 + hotfix PR #4)
• Atualizar Jira (PROJ-16 e subtasks PROJ-33/34/35/36 estavam "A fazer" apesar do trabalho pronto) → feito nesta sessão
• Criar docs de sprint e QA de API retroativas (Sprint 0 e 1) → feito nesta sessão
• Iniciar Sprint 2 (Clientes e Equipes) → Backend, só após confirmação explícita
