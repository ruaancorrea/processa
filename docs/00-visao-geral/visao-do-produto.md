# Visão do Produto

## 1. Problema

Escritórios de contabilidade brasileiros operam dezenas de processos recorrentes por cliente — fechamento fiscal mensal, apuração de impostos, folha de pagamento, abertura e alteração de empresas, admissão e demissão de funcionários, entrega de obrigações acessórias (SPED, DCTF, eSocial). Esses processos são previsíveis e repetitivos, mas na prática do dia a dia costumam ser controlados fora de qualquer sistema:

- Planilhas paralelas para saber "quem está fazendo o quê", mantidas por controle pessoal de cada gestor;
- Grupos de WhatsApp para cobrar documento de cliente, sem rastro nem histórico;
- E-mails soltos que se perdem entre dezenas de clientes simultâneos;
- Sistemas contábeis (Domínio, Alterdata, SAGE, Onvio) que resolvem o **cálculo fiscal**, mas não a **gestão da rotina operacional** — quem é responsável, até quando, o que falta do cliente.

O resultado: prazos perdidos, retrabalho, dependência de memória individual e nenhuma visibilidade gerencial real sobre a operação.

## 2. Visão

> Processa é a camada de gestão operacional que fica entre o sistema contábil e a equipe: transforma processos que hoje vivem em planilhas e grupos de WhatsApp em fluxos configuráveis, executáveis e auditáveis — com um portal onde o cliente final também participa, em vez de ficar de fora.

Não é um ERP contábil (não calcula impostos, não gera guias, não substitui Domínio/Onvio/Alterdata) — Processa **orquestra o trabalho** ao redor desses sistemas, e pode futuramente integrar-se a eles via API.

## 3. Por que não é "um clone do Onvio Processos"

O Onvio Processos (Thomson Reuters) valida a categoria: gestão de projetos/processos, documentos e colaboração com clientes para escritórios contábeis é uma necessidade real de mercado. Processa parte da mesma categoria, mas com decisões de produto próprias:

| Decisão de produto | Motivação |
|---|---|
| Motor de processos com **8 tipos de etapa** (comum, condicional, automatizada, notificação, agendamento, subprocesso, conclusão, união) | Cobre desde tarefa manual simples até automação com API externa e fluxos paralelos (fork/join) — não apenas um checklist linear |
| **Motor de regras declarativo**, separado do desenho do fluxo | Escalonamento e alertas configuráveis sem redesenhar o processo inteiro |
| **Kanban nativo**, não só lista | A operação do dia a dia de um escritório se parece mais com um board de tarefas do que com uma lista de chamados |
| **Multi-tenant desde o dia 1** | Pensado como produto vendável a múltiplos escritórios, não como sistema interno de um único cliente |
| IA como **assistente sobre dados existentes**, não gerador de conteúdo genérico | Resumo de pendências do cliente, sugestão de fluxo a partir de texto, busca em linguagem natural — três casos concretos, não "IA por moda" |

## 4. Objetivos de negócio

1. Reduzir a tempo praticamente zero o intervalo entre "documento vence" e "documento é cobrado do cliente" (automação, não follow-up manual).
2. Dar ao gestor visibilidade de fila de trabalho por pessoa, cliente e tipo de processo em uma única tela.
3. Eliminar a pergunta "cadê o status disso?" — todo processo tem estado, dono e prazo visíveis a qualquer momento.
4. Viabilizar o produto como SaaS multi-tenant vendável a outros escritórios de contabilidade, não apenas como ferramenta de uso interno.

## 5. Não-objetivos (fora de escopo do MVP)

- Cálculo fiscal, geração de guias, folha de pagamento — integração futura com sistemas contábeis existentes, não reimplementação.
- Assinatura digital de documentos (avaliar integração com provedor especializado pós-MVP).
- Cliente oficial (web ou mobile) — Processa é um projeto API-only; qualquer cliente é consumidor externo da API.

## 6. Métricas de sucesso do produto

| Métrica | Meta pós-piloto |
|---|---|
| Processos com responsável e prazo definidos automaticamente | > 90% |
| Tempo médio entre vencimento de documento e primeira cobrança automática ao cliente | < 1 hora |
| Taxa de adoção do portal do cliente (clientes que acessam ao menos 1x/mês) | > 60% |
| Redução de processos "órfãos" (sem responsável) em relação ao baseline pré-implantação | > 80% |

Ver personas e jornadas em [`personas-e-jornadas.md`](personas-e-jornadas.md) e o detalhamento funcional completo em [`../01-requisitos/requisitos-funcionais.md`](../01-requisitos/requisitos-funcionais.md).
