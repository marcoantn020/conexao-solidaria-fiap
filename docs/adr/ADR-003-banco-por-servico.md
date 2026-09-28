# ADR-003: PostgreSQL isolado por serviço (database-per-service)

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado exige um documento justificando por que os bancos de dados X e Y foram
escolhidos, sugerindo que cada microsserviço deve ter sua própria base. Os dados envolvidos
(usuários, campanhas, doações) são estruturados e possuem regras transacionais claras
(ex.: meta financeira > 0, valor arrecadado consistente).

## Decisão

Cada microsserviço tem sua própria instância/base PostgreSQL: `CampanhasDb` para o
`Api.CampanhasUsuarios` (usuários, campanhas) e `DoacoesDb` para o `Worker.Doacoes` (ledger
de doações processadas). Mesma tecnologia nos dois serviços, isolamento por propriedade de
dados (data ownership), não por tipo de tecnologia.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Banco único compartilhado entre os dois serviços | Mais simples de implementar; sem duplicação de infraestrutura | Viola o princípio de autonomia de dados de microsserviços (acoplamento forte via schema compartilhado); um serviço pode quebrar o outro ao migrar schema | Contradiz a própria razão de ser da arquitetura de microsserviços escolhida na ADR-001 |
| Polyglot: PostgreSQL + MongoDB | Demonstra conhecimento de polyglot persistence | Dados de doação são inerentemente relacionais (valor, campanha, timestamp) — não há necessidade funcional de um modelo de documento; adiciona uma tecnologia extra para operar/observar em Kubernetes sem ganho real, sob prazo apertado | Complexidade sem justificativa técnica genuína — risco de soar artificial na avaliação |
| PostgreSQL isolado por serviço (database-per-service) | Respeita autonomia de dados de cada microsserviço; consistência ACID para valores financeiros; time do hackathon já domina PostgreSQL, reduzindo risco de execução | Requer um mecanismo de sincronização entre os dois bancos para refletir o total arrecadado (ver ADR-004) | — (escolhida) |

## Consequências

**Positivas**
- Cada serviço evolui seu schema de forma independente, sem acoplamento via banco
- Consistência transacional forte onde importa (valores financeiros) via ACID do PostgreSQL
- Justificativa técnica simples e defensável para o PDF exigido no enunciado: a escolha é
  sobre *isolamento de dados por serviço*, não sobre features específicas de um banco NoSQL

**Negativas / Trade-offs**
- O total arrecadado da campanha (dono: `CampanhasDb`) precisa ser atualizado a partir de um
  evento processado no `DoacoesDb` — exige um mecanismo de comunicação entre serviços em vez
  de uma simples transação de banco (ver ADR-004)
- Dois bancos para provisionar, migrar e observar em vez de um
