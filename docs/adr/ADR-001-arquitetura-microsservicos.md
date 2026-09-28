# ADR-001: Arquitetura de microsserviços em vez de monolito modular

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado do hackathon exige explicitamente uma arquitetura de no mínimo dois
microsserviços distintos, comunicando-se de forma assíncrona via broker de mensageria, e é
um critério obrigatório de avaliação — não apenas o funcionamento da aplicação, mas a
arquitetura aplicada. A ideia inicial do grupo era um monolito modular, que teria boa
organização interna mas não atenderia ao requisito de múltiplos serviços implantáveis
independentemente.

## Decisão

Adotar arquitetura de microsserviços no nível de sistema, com exatamente dois serviços no
MVP: `Api.CampanhasUsuarios` (autenticação/RBAC, campanhas, doadores, painel público) e
`Worker.Doacoes` (consumidor assíncrono de doações). Modularidade continua sendo aplicada,
mas como princípio de design *dentro* de cada serviço (módulos internos separados por
responsabilidade), não como estilo arquitetural do sistema como um todo.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Monolito modular | Menor complexidade operacional; deploy único; sem latência de rede entre módulos | Não atende ao requisito obrigatório de ≥2 microsserviços comunicando-se assincronamente | Reprovaria o critério de arquitetura, independente da qualidade da modularização interna |
| Microsserviços granulares (>2, um por entidade) | Isolamento máximo de responsabilidades | Overhead operacional desproporcional ao prazo do hackathon; mais manifests k8s, mais pontos de falha na demo | YAGNI — o enunciado pede o mínimo de 2 e não há necessidade funcional de mais granularidade no MVP |
| 2 microsserviços (API + Worker) | Atende ao requisito mínimo; separa claramente o caminho síncrono (gestão/consulta) do assíncrono (processamento de doação); escopo administrável no prazo | Fronteira de dados entre os dois serviços exige um mecanismo de atualização cross-service (ver ADR-004) | — (escolhida) |

## Consequências

**Positivas**
- Atende ao critério obrigatório de avaliação de arquitetura
- Caminho síncrono (API) e assíncrono (Worker) ficam claramente separados, facilitando o
  roteiro de demonstração exigido no enunciado
- Cada serviço pode ter seu próprio ciclo de deploy/escala no Kubernetes

**Negativas / Trade-offs**
- Introduz complexidade de comunicação entre serviços (rede, serialização, eventual
  consistência) que um monolito não teria
- Exige coordenação de contrato entre o evento publicado pela API e o consumido pelo Worker
- Mais manifests Kubernetes e mais serviços para observar/depurar
