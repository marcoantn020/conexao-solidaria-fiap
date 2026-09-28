# ADR-002: RabbitMQ como broker de mensageria

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

O enunciado exige que, ao receber uma doação, a API publique um evento
(`DoacaoRecebidaEvent`) em um broker de mensageria (RabbitMQ ou Kafka), consumido por um
segundo serviço que atualiza o valor arrecadado da campanha. É preciso escolher entre os
dois brokers citados, considerando o prazo do hackathon e a necessidade de rodar tudo em um
cluster Kubernetes local.

## Decisão

Usar RabbitMQ como broker de mensageria, com uma fila dedicada (`doacao-recebida`) para o
evento `DoacaoRecebidaEvent`.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Kafka | Alta capacidade de throughput e replay de eventos; padrão de mercado para streaming | Requer mais recursos (e histórico de operação com Zookeeper/KRaft) para rodar em Minikube; complexidade de operação desproporcional a um fluxo de evento único/consumidor único | O ganho de throughput/replay não é necessário para o volume de doações de um MVP de demo |
| RabbitMQ | Leve, simples de subir em k8s local; modelo fila/consumidor direto mapeia 1:1 com o requisito (publica evento → um worker consome); interface de gestão facilita mostrar a mensagem passando pela fila no vídeo de demo | Menos adequado a cenários de streaming/replay de longo prazo (não é o caso aqui) | — (escolhida) |

## Consequências

**Positivas**
- Setup mais rápido e leve em Minikube, reduzindo risco operacional no prazo do hackathon
- Management UI do RabbitMQ atende diretamente ao requisito do vídeo de demo ("abrir a
  interface do RabbitMQ/Kafka mostrando a mensagem passando pela fila")
- Modelo de fila simples é suficiente para um único tipo de evento e um único consumidor

**Negativas / Trade-offs**
- Caso o sistema evolua para múltiplos consumidores/replay de eventos históricos, uma
  migração para Kafka seria necessária no futuro
