# Personas e Jornadas

## 1. Personas internas (equipe do escritório)

### 1.1 Administrador — "Marcos, sócio do escritório"

- Responsável pela operação como um todo: configura tipos de processo, equipes, integrações, e acompanha todos os departamentos.
- Não executa tarefas operacionais no dia a dia — sua interação principal é com dashboards e configuração.
- Dor principal: não ter visão consolidada de gargalos entre os departamentos (fiscal, folha, societário).

### 1.2 Gestor — "Camila, supervisora fiscal"

- Coordena uma equipe de 6 analistas. Distribui demandas, cobra prazos, cobre ausências.
- Interage com o sistema várias vezes ao dia: painel de demandas, kanban, atribuição manual.
- Dor principal: descobrir um atraso só quando o cliente liga reclamando — quer saber antes.

### 1.3 Analista — "Pedro, analista fiscal pleno"

- Executa as demandas atribuídas a ele: confere documentos, processa apurações, registra o que falta.
- Interage principalmente com sua fila pessoal (kanban ou lista) e com o formulário de execução de etapa.
- Dor principal: perder tempo entrando em 3 sistemas diferentes (e-mail, WhatsApp, sistema fiscal) para juntar informação de um único cliente.

## 2. Persona externa

### 2.1 Cliente final — "Fernanda, dona de uma pequena empresa cliente do escritório"

- Não quer aprender um sistema. Quer saber, em uma tela simples: o que falta enviar, para quando, e se está tudo em dia.
- Acessa o portal esporadicamente, geralmente após receber uma notificação (e-mail ou WhatsApp).
- Dor principal: não saber se o documento que mandou por WhatsApp semana passada chegou a alguém, ou se sumiu.

## 3. Jornadas críticas

### 3.1 Jornada: fechamento fiscal mensal (Camila, gestora)

1. Todo dia 1º, o sistema abre automaticamente uma demanda de "Fechamento Fiscal Mensal" para cada cliente ativo da equipe Fiscal (processo recorrente).
2. Camila abre o painel de demandas: vê a lista ordenada por prioridade e prazo, com destaque para as sem responsável.
3. Usa atribuição manual para os 3 clientes mais críticos (vê experiência e recorrência de atendimento na tabela de membros) e deixa o resto no modo dinâmico.
4. Ao longo do mês, acompanha pelo kanban: quantos estão em "aguardando documentos", quantos em "processamento", quantos em "revisão".
5. No dia 25, a Central de Pendências mostra 4 processos "em alerta" (80% do prazo consumido) — Camila intervém antes de virarem atraso.

### 3.2 Jornada: cobrança automática de documento (sistema + Fernanda, cliente)

1. O processo de Fernanda entra na etapa "Solicitar Documentos" — dispara automaticamente uma notificação por e-mail e WhatsApp para os contatos da empresa.
2. Uma regra de automação está configurada: "se documento não enviado em 3 dias → notificação de lembrete + escalonamento para o gestor".
3. Fernanda recebe o lembrete, acessa `cliente.processa.app`, faz login, vê a pendência e envia o XML pelo portal.
4. O sistema valida o upload, marca o documento como recebido, e a etapa avança automaticamente para "Conferir Documentos" — sem ninguém do escritório precisar acompanhar manualmente.

### 3.3 Jornada: análise gerencial (Marcos, administrador)

1. Marcos abre o dashboard consolidado: vê tempo médio de execução por etapa, taxa de SLA cumprido por equipe, volume de processos por tipo.
2. Identifica que a equipe Societário está com tempo médio de "Abertura de Empresa" 40% acima do configurado.
3. Consulta o log de auditoria filtrado por aquele tipo de processo para entender em qual etapa o gargalo se concentra.
4. Ajusta a configuração do fluxo (adiciona um responsável fixo em vez de manual na etapa gargalo) diretamente na tela de configuração — sem precisar de desenvolvedor.

Estas jornadas orientam a priorização de funcionalidades no [roadmap do MVP](../07-roadmap/roadmap-mvp.md) e o desenho das telas descrito nos [requisitos funcionais](../01-requisitos/requisitos-funcionais.md).
