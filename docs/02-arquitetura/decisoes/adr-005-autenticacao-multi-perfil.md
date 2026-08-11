# ADR-005 — JWT + RBAC Multi-perfil, Refresh Token Rotativo

**Status:** Aceito · **Data:** 2026-08-10

## Contexto

Processa tem dois públicos de autenticação completamente distintos — usuários internos do escritório (admin/gestor/analista) e clientes externos no portal — que não podem compartilhar espaço de identidade nem escopo de permissões.

## Decisão

- **JWT** (access token, expiração 15 min) + **refresh token rotativo** (7 dias, invalidado a cada uso e reemitido — mitiga replay de refresh token roubado).
- Claims do access token incluem `tenant_id`, `user_id`, `perfil` (admin/gestor/analista) e, para usuários com múltiplas equipes, a lista de `equipe_id` com o papel em cada uma.
- **RBAC** verificado por policy do ASP.NET Core (`[Authorize(Policy = "GestorOuAdmin")]`), nunca checagem manual de string de perfil espalhada pelos controllers.
- **Portal do cliente** usa um esquema de autenticação **separado** (`ClientePortalAuthenticationScheme`), com seu próprio emissor de token, sem nenhuma claim ou escopo em comum com o esquema interno — um token de cliente jamais é aceito em uma rota interna e vice-versa, validado por testes de integração dedicados.
- Bloqueio de conta após 5 tentativas de login falhas consecutivas, 15 minutos, contador resetado no login bem-sucedido.

## Alternativas consideradas

- **Sessão em memória/cookie stateful:** rejeitado — incompatível com a exigência de API stateless (ver visão arquitetural) e com múltiplas instâncias da API atrás de load balancer sem sticky session.
- **Um único esquema de auth para interno e portal, diferenciado só por claim:** rejeitado — um bug de autorização nesse cenário vaza acesso interno para um cliente externo; esquemas fisicamente separados tornam essa classe de bug estruturalmente impossível, não apenas "testada para não acontecer".

## Consequências

- Dois pipelines de autenticação para manter, mas com superfícies de risco isoladas — trade-off aceito conscientemente por segurança.
- Testes de integração de autorização são obrigatórios no CI para todo novo endpoint (gate de PR, não opcional).
- Refresh token rotativo exige armazenar o token (ou seu hash) em Redis com TTL, para permitir invalidação e detecção de reuso (reuse detection — se um refresh token já usado for reapresentado, todos os tokens da sessão são revogados).
