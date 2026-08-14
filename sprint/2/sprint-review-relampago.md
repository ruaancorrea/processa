SPRINT 2 — CADASTRO DE CLIENTES E EQUIPES (PROJ-17) — 2026-08-13/14

ENTREGAS:
• CRUD de equipes e membros com papel Gestor/Analista (PROJ-37) → OK
• CRUD de clientes e grupos de clientes, CNPJ único por tenant (PROJ-38) → OK
• CRUD de contatos do cliente (PROJ-39) → OK
• Vínculo responsável-cliente por equipe, com soft-delete reativável (PROJ-40) → OK
• Novo módulo Processa.Modules.Clientes (Domain/Application/Infrastructure/Presentation completos) → OK
• Cnpj e Email movidos de Identidade.Domain pra Shared.Kernel → OK
• Nova regra de arquitetura: nenhum módulo pode referenciar outro diretamente (24 testes, antes 18) → OK
• 131 testes unitários (antes 63), 35 de integração (antes 15), 24 de arquitetura → OK

DECISÕES:
• Equipe/MembroEquipe entram em Identidade (não em Clientes) → mesmo padrão do projeto de referência, Equipe é sobre organizar Usuario
• Cnpj/Email viram Shared.Kernel → dois módulos (Tenant e Cliente) precisavam da mesma validação sem duplicar o algoritmo
• Comunicação entre módulos via interface em Shared.Kernel (IVerificadorMembroEquipe), não ISender cross-module → evita ProjectReference entre Clientes e Identidade, que recriaria acoplamento de compilação
• Cnpj em Cliente mapeado via HasConversion, não OwnsOne → índice composto (tenant_id, cnpj) não compõe direto num HasIndex lambda com owned type navigation

BUGS (achados na revisão, mesma classe do achado de CNPJ do Sprint 1):
• Papel=Admin ao adicionar membro de equipe → 422 sem o dict `errors` documentado → Raiz: regra só existia no Domain, nunca passava pelo FluentValidation → Fix: `.NotEqual(Perfil.Admin)` no validator do comando
• Contato sem nenhum meio de contato → mesmo problema → Raiz: regra só no Domain (ContatoCliente.Criar) → Fix: `.Must()` de campo cruzado no validator

BLOQUEIOS:
• Nenhum bloqueio externo

MÉTRICAS:
• Arquivos: 101 (+4.399/-38) | Testes: 96→172 backend (24 arquitetura + 131 unitários + 35 integração, oito unit tests eram já existentes movidos) | Cobertura: 83,6%→83,34% linha (gate: 80%)
• Migrations novas: 2 (AdicionarEquipes em Identidade, CriarClientes — 1ª migration do módulo Clientes)
• Rodadas de revisão: 1 (5 skills) + verificação manual ponta a ponta completa (tenant→equipe→membro→cliente→contato→responsável)

NEXT:
• Abrir PR de feature/processa-sprint2-clientes-equipes → develop
• Atualizar Jira (PROJ-17 e subtasks PROJ-37/38/39/40) → feito nesta sessão, ao longo do trabalho (não só no final, lição do Sprint 1)
• Iniciar Sprint 3 (Motor de Processos I: Configuração, PROJ-18) → Backend, só após confirmação explícita
