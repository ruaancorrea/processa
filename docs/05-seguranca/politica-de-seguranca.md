# Política de Segurança

## 1. Autenticação e autorização

- **JWT:** access token 15 min, refresh token rotativo 7 dias (detecção de reuso revoga a sessão inteira) — ver [ADR-005](../02-arquitetura/decisoes/adr-005-autenticacao-multi-perfil.md).
- **RBAC** por policy do ASP.NET Core, verificado em toda rota autenticada.
- **Esquemas de autenticação isolados** para usuários internos e portal do cliente — um token nunca é válido no outro contexto.
- **Tokens de API:** escopos granulares, expiração configurável, exibidos uma única vez na criação, armazenados como hash SHA-256.
- **Rate limiting:** por IP (global, `AspNetCoreRateLimit` ou equivalente) e por usuário autenticado (por endpoint, mais restritivo em rotas de mutação).
- **Bloqueio de conta:** 5 tentativas de login falhas consecutivas → bloqueio de 15 minutos, contador resetado no login bem-sucedido.

## 2. Proteção de dados

- Senhas: bcrypt, cost factor ≥ 12.
- Credenciais de integrações externas (Gmail, Calendar, WhatsApp): cifradas com AES-256-GCM antes de persistir; chave de cifragem fora do banco (variável de ambiente / secret manager).
- Campos sensíveis (`senha_hash`, `token_hash`, credenciais de integração) nunca retornados em nenhuma resposta de API, inclusive em endpoints administrativos — mapeamento de DTO exclui esses campos na origem, não por filtro posterior.
- HTTPS obrigatório em produção (TLS 1.2+); HSTS habilitado.
- CORS restrito a origens explicitamente autorizadas por tenant (domínio do painel + domínio do portal do cliente).

## 3. Multi-tenancy

- Isolamento de dados garantido por Global Query Filter do EF Core em toda entidade com `tenant_id` — ver [ADR-002](../02-arquitetura/decisoes/adr-002-multi-tenancy.md). `IgnoreQueryFilters()` é proibido em código de aplicação (permitido só em contexto de job administrativo interno, sinalizado em code review).
- Testes de integração dedicados verificam que um usuário do Tenant A nunca consegue ler ou escrever dado do Tenant B, mesmo manipulando IDs diretamente na requisição.

## 4. Validação de input

- Toda entrada validada via **FluentValidation** antes de chegar à camada de domínio.
- Proteção contra SQL injection: EF Core com queries parametrizadas, nunca SQL concatenado.
- Proteção contra XSS: sanitização do corpo HTML de notificações (gerado por um editor rich text controlado pelo próprio usuário do escritório) na gravação, já que a API não controla como cada cliente consumidor vai renderizar esse conteúdo.
- Limite de tamanho em uploads (por tipo de arquivo e por plano de tenant) e em campos de texto livre.
- Validação de formato para CNPJ, e-mail, UUID nos DTOs de entrada.

## 5. Auditoria

- Log imutável de create/update/delete em toda entidade crítica: usuário (nullable para ação automática do sistema), dados antes/depois (snapshot JSON), IP, timestamp.
- Retenção mínima de 90 dias, configurável por plano.
- Ações automáticas (regras de automação, integrações) registradas com origem explícita — nunca aparentam ter sido feitas por um usuário humano.

## 6. Secrets e ambientes

- Nenhum secret versionado em repositório — `.env` sempre no `.gitignore`, `.env.example` documenta as chaves esperadas sem valores reais.
- Ambientes segregados: development, staging, production, com credenciais e chaves de cifragem distintas por ambiente.
- Rotação periódica de tokens de integração OAuth2 (Gmail, Calendar).
- Scan de dependências (`dotnet list package --vulnerable`, `npm audit`) como etapa do CI — build falha em vulnerabilidade crítica sem exceção documentada.

## 7. LGPD

- Dados de clientes e contatos são dados pessoais de terceiros tratados pelo escritório (controlador) através do Processa (operador) — o contrato de uso do produto formaliza essa relação.
- Direito de exclusão: endpoint administrativo de anonimização de cliente/contato preserva o histórico de auditoria (obrigação legal) mas remove dado pessoal identificável dos registros operacionais.
- Portal do cliente expõe apenas o dado do próprio cliente autenticado — nunca de terceiros, nem do mesmo tenant.

## 8. Checklist de revisão de segurança (pré-piloto)

Executado no Sprint 12, antes do piloto com escritório real — ver [roadmap](../07-roadmap/roadmap-mvp.md):

- [ ] OWASP Top 10 revisado contra o código final do MVP
- [ ] Testes de penetração básicos (autenticação, autorização entre tenants, upload malicioso)
- [ ] Rate limiting validado sob carga
- [ ] Secrets de produção gerados e armazenados fora do repositório
- [ ] Backup e restore do banco testado ponta a ponta
