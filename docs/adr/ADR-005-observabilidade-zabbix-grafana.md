# ADR-005: Zabbix como coletor de métricas, Grafana como camada de visualização

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado exige observabilidade especificamente via Zabbix e Grafana (não Prometheus),
com métricas de saúde expostas em `/health` ou `/metrics` e ao menos um dashboard no Grafana
exibindo métricas reais da aplicação rodando (ex.: CPU/memória dos pods ou contagem de
requisições HTTP).

## Decisão

Usar o Zabbix como backend de coleta de métricas (via Zabbix Agent/Proxy monitorando os
pods e checks HTTP no endpoint `/health` de cada serviço), e o Grafana como camada de
visualização, conectado ao Zabbix através do plugin de datasource oficial Zabbix-Grafana,
com um dashboard mostrando métricas reais dos serviços em execução no cluster.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Prometheus + Grafana (stack mais comum em k8s) | Integração nativa e amplamente documentada com Kubernetes (kube-state-metrics, cAdvisor) | Não atende ao requisito literal do enunciado, que nomeia Zabbix explicitamente | Enunciado exige Zabbix, não é uma escolha livre de stack |
| Zabbix isolado (sem Grafana), usando seus próprios dashboards | Menos peças móveis | Não atende ao requisito de dashboard no Grafana especificamente | Enunciado exige um dashboard no Grafana |
| Zabbix como coletor + Grafana como visualização (via plugin de datasource) | Atende literalmente aos dois requisitos nomeados no enunciado; Zabbix cobre bem métricas de infraestrutura (CPU/memória dos pods); Grafana entrega o dashboard exigido | Combinação menos comum e menos documentada que Prometheus+Grafana; maior risco de integração no prazo curto | — (escolhida, mitigando o risco com uma validação antecipada — ver RN-02 no BRD) |

## Consequências

**Positivas**
- Atende literalmente ao requisito do enunciado (Zabbix **e** Grafana, ambos nomeados)
- Zabbix cobre monitoramento de infraestrutura (CPU/memória de pods) sem esforço adicional
  de instrumentação de código
- Grafana entrega uma visualização única e apresentável no vídeo de demonstração

**Negativas / Trade-offs**
- Stack menos documentada que Prometheus+Grafana, exigindo validação antecipada (spike) da
  integração do plugin de datasource Zabbix antes de depender dela na demo final
- Zabbix Server/Proxy é mais um componente para provisionar e observar dentro do cluster
