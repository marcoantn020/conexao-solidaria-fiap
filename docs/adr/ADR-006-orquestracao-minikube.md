# ADR-006: Minikube como cluster Kubernetes local

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado exige que a aplicação rode em um cluster Kubernetes local, citando Minikube,
Kind ou Docker Desktop K8s como opções aceitas, com entrega dos manifests
(Deployments/Services/ConfigMaps).

## Decisão

Usar Minikube como cluster Kubernetes local de desenvolvimento e para a gravação do vídeo
de demonstração.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Kind | Inicialização rápida; leve; popular em pipelines de CI | Dashboard e addons (metrics-server, ingress) exigem configuração manual adicional | Menor familiaridade do grupo/material do curso; sem ganho relevante para o escopo do hackathon |
| Docker Desktop K8s | Já integrado a quem usa Docker Desktop | Consome mais recursos da máquina; menos portável entre integrantes do grupo com setups diferentes | Depende de uma instalação específica (Docker Desktop) não garantida em todas as máquinas do grupo |
| Minikube | Addons prontos (`dashboard`, `metrics-server`) via um comando; amplamente documentado nos materiais do curso; comportamento previsível para gravar o `kubectl get pods` exigido no vídeo | Startup um pouco mais lento que Kind | — (escolhida) |

## Consequências

**Positivas**
- Addons nativos (`minikube addons enable metrics-server`) simplificam a alimentação de
  métricas reais para o Zabbix/Grafana (ADR-005)
- Documentação e suporte amplos, reduzindo risco de bloqueio técnico no prazo curto

**Negativas / Trade-offs**
- Requer mais recursos de máquina que Kind para os mesmos workloads
- Comandos e configurações específicos de Minikube não são 1:1 portáveis para um cluster
  gerenciado de produção (fora do escopo deste MVP)
