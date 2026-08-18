SPRINT 3 — MOTOR DE PROCESSOS I: CONFIGURAÇÃO — 2026-08-17

ENTREGAS:
• CRUD de tipos de processo (modos fixo/dinâmico/manual) → OK
• Campos personalizados (8 tipos, opções só p/ tipo Lista) → OK
• CRUD de fluxos com fluxo padrão único por tipo de processo → OK
• Permissões de início por perfil/usuário → OK (configurável; enforcement fica pro Sprint 5)

DECISÕES:
• Perfil promovido pra Shared.Kernel → Processos precisa validar perfil sem referenciar Domain de Identidade (3ª repetição do padrão Cnpj/Email)
• PermissoesInicio.Criar deixou de retornar Result<T> → nunca falhava de verdade, Result era abstração sem uso real
• PermissoesInicio e Opcoes mapeados via HasConversion (JSON), não OwnsOne → VO com construtor privado/sem setters não materializa via ToJson()

BUGS:
• Troca de fluxo padrão intermitentemente violava o índice único parcial → Raiz: EF Core não garante ordem UPDATE-antes-de-marcar dentro de um único SaveChanges → Fix: duas gravações (desmarcar+commit, depois marcar+commit) em CriarFluxoCommand e DefinirFluxoPadraoCommand
• CriarCampoPersonalizadoCommand sem espelho FluentValidation da regra "Lista exige opções" → Raiz: mesma classe recorrente (CNPJ Sprint1, papel/contato Sprint2) → Fix: RuleFor(x=>x).Must(...).WithName("Opcoes")
• DefinirPermissoesInicioCommand explodia com NullReferenceException se Perfis/UsuarioIds vinham null → Raiz: parâmetros não-nulláveis sem defesa → Fix: List<T>? + null-coalescing, mesmo padrão de CampoPersonalizado.Criar

BLOQUEIOS:
• Nenhum bloqueio real — Docker Desktop precisou start manual e .env tinha POSTGRES_PORT=5433 desalinhado do appsettings.Development.json (5432), ambos resolvidos localmente

MÉTRICAS:
• Arquivos: 65 | Testes: 190→272 (24 arquitetura + 196 unitários + 52 integração) | Cobertura: 83,34%→83,3%

NEXT:
• Sprint 4 — Motor de Processos II (as 8 tipo-etapa, PROJ-19) — só ao "segue" explícito
