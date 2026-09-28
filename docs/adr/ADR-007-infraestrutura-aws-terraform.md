# ADR-007: Infraestrutura AWS complementar via Terraform em repositório separado

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado do hackathon exige apenas um cluster Kubernetes local (Minikube/Kind/Docker
Desktop K8s — ver [ADR-006](ADR-006-orquestracao-minikube.md)). O grupo decidiu ir além do
exigido e também disponibilizar a aplicação publicamente na internet, rodando em um cluster
Kubernetes hospedado na AWS, com bancos de dados gerenciados (RDS) — mantendo o ambiente
local (Minikube) como alvo primário para desenvolvimento e para a gravação do vídeo de
demonstração. É necessário decidir: (1) que tipo de cluster Kubernetes usar na AWS, (2) como
tratar os bancos de dados na nuvem, e (3) onde versionar a infraestrutura como código.

Restrição explícita do grupo: o ambiente AWS deve ser fácil e rápido de subir e, principalmente,
fácil e rápido de derrubar — para evitar custo contínuo desnecessário em um ambiente que só
precisa existir durante avaliação/demonstração.

## Decisão

- **Cluster**: k3s (distribuição leve de Kubernetes) rodando em uma única instância EC2,
  provisionada via Terraform. Não usar EKS.
- **Bancos de dados**: uma única instância RDS PostgreSQL (`db.t4g.micro`, single-AZ, sem
  acesso público — apenas a partir do Security Group do cluster) hospedando duas databases
  lógicas separadas (`campanhas` e `doacoes`), preservando o isolamento por serviço da
  [ADR-003](ADR-003-banco-por-servico.md) sem pagar por duas instâncias RDS.
- **RabbitMQ, Zabbix e Grafana** continuam rodando dentro do cluster via manifests Kubernetes
  (não como serviços gerenciados da AWS), mantendo consistência com a
  [ADR-002](ADR-002-mensageria-rabbitmq.md)/[ADR-005](ADR-005-observabilidade-zabbix-grafana.md)
  e reduzindo custo.
- **Infraestrutura como código**: Terraform, em um **repositório Git separado** do código da
  aplicação (não neste repositório). `terraform apply` provisiona tudo (VPC, EC2 com k3s, RDS,
  ECR, IAM/Security Groups); `terraform destroy` derruba tudo, incluindo os bancos — os dados
  não persistem entre ciclos de subida/derrubada, o que é aceitável para um ambiente de
  demonstração.
- **Manifests Kubernetes** continuam vivendo neste repositório (junto com o código que
  descrevem), estruturados com Kustomize (`base/` + overlay `local` + overlay `aws`), para
  reusar a mesma base entre o Minikube local e o cluster AWS, variando apenas configuração
  (connection strings, tipo de Service, imagens do ECR vs. build local).

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| EKS (Kubernetes gerenciado da AWS) | Padrão de mercado; mais robusto; integração nativa com IAM/ALB | Control plane custa ~US$0,10/h fixo mesmo com o cluster ocioso; provisionamento/destruição leva 15-20 min | Contradiz diretamente o requisito de "fácil e rápido de subir/derrubar"; o custo fixo do control plane não se justifica para um ambiente ligado só durante avaliação |
| k3s em uma única EC2 | Cluster Kubernetes real (mesma API, mesmo `kubectl`); sobe/derruba em minutos; custo é só o da instância (pode ser interrompida/destruída a qualquer momento) | Não é "gerenciado" pela AWS; um único nó é um ponto único de falha (aceitável para demo, não para produção) | — (escolhida) |
| RDS: uma instância por serviço (duas instâncias) | Isolamento físico mais estrito, mais fiel ao espírito original da ADR-003 | Dobra o custo fixo de RDS | Para o objetivo de demo/avaliação, isolamento lógico (bancos separados na mesma instância) já preserva a propriedade de dados por serviço sem acesso cruzado; custo duplicado não se justifica |
| RDS: um único banco compartilhado entre os serviços | Mais barato ainda | Viola a ADR-003 (isolamento de dados por serviço) | Contradiz decisão já tomada e válida |
| Terraform no mesmo repositório da aplicação | Um único repositório para gerenciar | Mistura ciclo de vida de infraestrutura (raramente muda) com ciclo de vida de aplicação (muda a cada feature); CI de aplicação não deveria ter permissão de alterar infraestrutura de produção por padrão | Separação de responsabilidades e de permissões — o pipeline de CI da aplicação só precisa buildar e publicar imagens, não provisionar VPC/IAM |
| Postgres/RabbitMQ/Zabbix/Grafana como serviços gerenciados da AWS (RDS para tudo, Amazon MQ para RabbitMQ, etc.) | Menos operação manual | Custo bem mais alto; complexidade de Terraform maior; nenhum requisito do hackathon pede serviços gerenciados especificamente | Mistura desnecessária de custo e complexidade para o objetivo de demo — os serviços já rodam bem em Kubernetes |

## Consequências

**Positivas**
- Demonstra domínio de infraestrutura como código e de operação de Kubernetes fora do ambiente local, além do exigido pelo enunciado
- Ciclo de vida rápido e barato (minutos para subir, minutos para derrubar), reduzindo risco de esquecer recursos ligados gerando custo
- Repositório de infraestrutura separado permite reuso futuro (outro projeto poderia usar o mesmo módulo Terraform) e isola permissões de infraestrutura do pipeline de CI da aplicação
- Reuso dos mesmos manifests Kubernetes entre local e AWS (via Kustomize) evita duplicar definições de Deployment/Service

**Negativas / Trade-offs**
- k3s em nó único não tem alta disponibilidade — se a instância cair, o cluster inteiro cai. Aceitável para demo, não para produção
- Dados no RDS não persistem entre ciclos de `apply`/`destroy` (sem snapshot automático) — cada subida começa com bancos vazios, exigindo novo seed de dados de demonstração
- Dois repositórios exigem coordenação manual: uma mudança de infraestrutura (ex.: novo Security Group) não é automaticamente refletida na aplicação, e vice-versa
- Exige credenciais AWS configuradas por quem for rodar `terraform apply`/`destroy` — não é algo que o assistente de IA pode provisionar sozinho (conta AWS, billing, credenciais são responsabilidade do grupo)
