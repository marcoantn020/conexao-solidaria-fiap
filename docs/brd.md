# Business Requirements Document (BRD) — Conexão Solidária

| Campo | Valor |
|---|---|
| Projeto | Conexão Solidária — Plataforma de Gestão de Doações |
| Solicitante | ONG Esperança Solidária (enunciado Hackathon Pós-Tech) |
| Responsável (PM/Arquitetura) | Grupo do Hackathon |
| Data | 2026-08-29 |
| Versão | 1.0 |
| Status | Aprovado para desenvolvimento |
| Repositório de documentação | `docs/` neste repositório |

## 1. Contexto e Problema

A ONG Esperança Solidária atua há mais de 10 anos acolhendo crianças em situação de
vulnerabilidade. A gestão de doadores e campanhas de arrecadação é hoje feita de forma
manual, o que limita a capacidade de expansão da ONG. Não há visibilidade pública em
tempo real sobre o andamento das campanhas, o que reduz a confiança de potenciais
doadores e a transparência da operação.

## 2. Objetivo de Negócio

Entregar um MVP da plataforma digital "Conexão Solidária" que permita à ONG gerenciar
campanhas de arrecadação e doadores digitalmente, com um painel público de transparência
mostrando o total arrecadado em tempo real, sobre uma arquitetura pronta para escalar
(microsserviços, mensageria assíncrona, observabilidade e automação de deploy).

## 3. Escopo

**Dentro do escopo (MVP):**
- Autenticação e autorização baseada em roles (GestorONG, Doador)
- CRUD de campanhas (GestorONG)
- Cadastro público de doadores
- Painel público de transparência (campanhas ativas + valor arrecadado)
- Processamento assíncrono de doações via fila de mensageria
- Deploy em Kubernetes local com pipeline de CI

**Fora do escopo (explicitamente não tratado neste MVP):**
- Processamento real de pagamento (gateway de pagamento, PIX, cartão) — o fluxo trata
  apenas a *intenção* de doação e sua contabilização
- Múltiplos idiomas
- Aplicativo mobile
- Recuperação de senha / fluxo completo de gestão de conta
- Deploy em nuvem pública / ambiente produtivo real

## 4. Stakeholders

| Papel | Responsabilidade |
|---|---|
| Diretoria da ONG (GestorONG) | Cria e administra campanhas |
| Doador | Cadastra-se e realiza doações |
| Grupo do Hackathon | Arquitetura, desenvolvimento e entrega do MVP |
| Professores/Avaliadores | Avaliam arquitetura, funcionamento e documentação |

## 5. Requisitos Funcionais

| ID | Descrição | Prioridade |
|---|---|---|
| RF-01 | Autenticação via JWT com roles `GestorONG` e `Doador` | Must |
| RF-02 | Endpoints de gestão restritos à role `GestorONG` | Must |
| RF-03 | Criar/editar campanha (Título, Descrição, DataInicio, DataFim, MetaFinanceira, Status) | Must |
| RF-04 | Regra: campanha não pode ter DataFim no passado nem MetaFinanceira ≤ 0 | Must |
| RF-05 | Cadastro público de doador (Nome, Email único, CPF válido, Senha com hash) | Must |
| RF-06 | Listagem pública de campanhas com status Ativa, exibindo Título, Meta e Valor Arrecadado | Must |
| RF-07 | Doador logado envia intenção de doação (IdCampanha, ValorDoacao) | Must |
| RF-08 | Regra: doação recusada para campanha Encerrada/Cancelada | Must |
| RF-09 | Doação processada de forma assíncrona (evento em fila, não escrita direta no banco) | Must |

## 6. Requisitos Não-Funcionais

| ID | Categoria | Métrica/Alvo |
|---|---|---|
| RNF-01 | Arquitetura | Mínimo 2 microsserviços com comunicação assíncrona via broker |
| RNF-02 | Observabilidade | `/health` ou `/metrics` expostos; dashboard Grafana com métricas reais |
| RNF-03 | Segurança | Senhas com hash (BCrypt); segredos fora de ConfigMap (K8s Secret) |
| RNF-04 | Implantação | Aplicação executável em cluster Kubernetes local via manifests versionados |
| RNF-05 | Automação | Pipeline de CI disparado a cada push na branch principal, gerando imagem Docker |

## 7. Critérios de Aceite

| ID | Critério |
|---|---|
| CA-01 | Repositório público com README passo a passo para subir infraestrutura e app localmente |
| CA-02 | Diagrama de arquitetura mostrando microsserviços, bancos, broker e observabilidade |
| CA-03 | PDF/documento justificando a escolha dos bancos de dados (ver `docs/architecture.md`, seção 8) |
| CA-04 | Vídeo de demonstração (≤15min) cobrindo: diagrama, pipeline de CI, pods no k8s, dashboard Grafana, fluxo completo de doação (JWT → campanha → doação → fila → worker → total atualizado) |
| CA-05 | Relatório de entrega com dados do grupo e links |

## 8. Processo As-Is / To-Be

- **As-Is**: gestão manual de doadores e campanhas (planilhas/processos informais), sem
  visibilidade pública do progresso das arrecadações.
- **To-Be**: plataforma digital com API de campanhas/doadores, processamento assíncrono de
  doações e painel público de transparência atualizado em tempo real.

## 9. Restrições e Premissas

- Prazo do hackathon é fixo e curto — decisões técnicas priorizam simplicidade defensável
  sobre sofisticação (ver ADRs em `docs/adr/`).
- Ambiente de execução é local (Minikube/Kind/Docker Desktop K8s), não produção real.
- Stack obrigatória: .NET (definida pelo requisito de pipeline "`.NET build`" no enunciado).
- Premissa: usuários `GestorONG` são provisionados via seed/migration, pois o enunciado não
  define cadastro público para esse perfil.

## 10. Riscos de Negócio

| ID | Descrição | Probabilidade | Impacto | Mitigação |
|---|---|---|---|---|
| RN-01 | Prazo curto do hackathon vs. escopo técnico exigido (k8s + mensageria + observabilidade) | Alta | Alto | Escopo tecnológico deliberadamente enxuto (2 serviços, sem Gateway/testes extras além do essencial) |
| RN-02 | Integração Zabbix→Grafana é incomum e pouco documentada comparada a Prometheus+Grafana | Média | Médio | Validar a integração cedo (spike) antes de depender dela no dia da demo |

## 11. Custo Estimado

Não aplicável — MVP acadêmico sem custos de infraestrutura em nuvem (execução local).

## 12. Justificativa de Negócio

Demonstrar competência de arquitetura de software distribuído (microsserviços, mensageria,
observabilidade, orquestração e automação) aplicada a um problema real de impacto social,
atendendo aos critérios de avaliação do Hackathon da Pós-Tech.
