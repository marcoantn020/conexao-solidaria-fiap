# ADR-004: Worker atualiza saldo da campanha via chamada HTTP interna à API

| Campo | Valor |
|---|---|
| Status | Aceita |
| Data | 2026-08-29 |
| Autor | Grupo do Hackathon |
| Decisores | Grupo do Hackathon |

## Contexto

Com bancos isolados por serviço (ADR-003), o `Worker.Doacoes` não tem acesso direto ao
`CampanhasDb`, que é onde vive o campo `ValorArrecadado` da campanha. Ainda assim, o
enunciado exige que, após consumir a fila, "um segundo serviço (Worker/Consumer) deve
consumir essa fila e, então, atualizar o Valor Total Arrecadado da respectiva campanha".

## Decisão

O `Worker.Doacoes`, ao consumir `DoacaoRecebidaEvent`, chama primeiro um endpoint HTTP
interno da API (`PATCH /internal/campanhas/{id}/arrecadado`) para que a própria
`Api.CampanhasUsuarios` atualize o valor em seu banco, e só então persiste a doação em seu
próprio banco (`DoacoesDb`, como ledger/auditoria) — ou seja, chamada primeiro, persistência
local depois, não o inverso. A API continua sendo a única dona de escrita do `CampanhasDb`.

- Essa ordem é deliberada: se o callback falhar, nada deve ficar persistido localmente, para
  que uma reentrega do RabbitMQ reprocesse a operação inteira do zero. A existência da linha
  no ledger é o que comprova que a doação foi totalmente processada (chamada HTTP concluída
  com sucesso); persistir antes da chamada abriria uma janela onde a doação parece
  processada mas o total da campanha nunca foi atualizado.

## Alternativas Consideradas

| Alternativa | Prós | Contras | Por que não foi escolhida |
|---|---|---|---|
| Worker escreve diretamente no `CampanhasDb` (banco compartilhado) | Implementação mais simples, sem chamada de rede extra | Quebra o isolamento de dados definido na ADR-003; acopla o schema dos dois serviços | Contradiz a decisão de database-per-service |
| Worker publica um segundo evento (`SaldoAtualizadoEvent`) que a API consome | Mantém tudo assíncrono/coreografado, sem chamada síncrona entre serviços | Adiciona uma segunda fila e um segundo consumidor só para o MVP; mais partes móveis para depurar sob prazo curto, sem ganho perceptível na demo exigida | Complexidade extra não paga seu custo no escopo do hackathon |
| Worker chama endpoint HTTP interno da API para atualizar o saldo | Cada serviço mantém propriedade exclusiva de escrita do seu banco; implementação simples (uma chamada HTTP); fácil de mostrar no vídeo de demo (fila → worker → API → painel público atualizado) | Introduz uma dependência síncrona pontual do Worker em relação à disponibilidade da API | — (escolhida) |

## Consequências

**Positivas**
- Cada serviço permanece dono exclusivo de escrita do seu próprio banco
- O fluxo continua satisfazendo o requisito de que a API não escreve o valor arrecadado
  diretamente ao *receber* a doação — a escrita final ainda acontece de forma assíncrona,
  disparada pelo Worker após consumir a fila
- Fluxo fácil de narrar no vídeo de demonstração (fila → worker → chamada interna → total
  atualizado no painel público)

**Negativas / Trade-offs**
- Se a API estiver indisponível no momento do callback, o Worker precisa de uma estratégia
  de retry (ex.: reentrega da mensagem/dead-letter queue) — tratado na implementação do
  consumer, não neste ADR
- Acopla o Worker a um contrato HTTP da API, além do contrato de evento da fila
- O RabbitMQ garante entrega "at-least-once": uma mensagem pode ser reentregue e gerar uma
  chamada duplicada ao callback. Por isso o contrato `AtualizarArrecadadoRequest` carrega
  `IdDoacao` (o mesmo id do `DoacaoRecebidaEvent`) especificamente para permitir deduplicação
  de reentregas
- A reentrega no `Worker.Doacoes` agora é limitada a exatamente uma nova tentativa por
  mensagem (via `eventArgs.Redelivered` no `DoacaoConsumer`); depois disso a mensagem é
  descartada (nack sem requeue) em vez de reentregar indefinidamente. E a deduplicação de
  `IdDoacao` está implementada do lado da `Api.CampanhasUsuarios` (tabela
  `CallbacksProcessados`): mesmo que a mesma doação chegue ao callback mais de uma vez, o
  `ValorArrecadado` só é incrementado na primeira
- O controle de acesso do endpoint interno é feito por um shared secret enviado no header
  `X-Internal-Token`, validado contra a configuração `Internal:SharedSecret` (comparação em
  tempo constante). Esse é o mecanismo concreto existente hoje no código — "alcançável
  apenas dentro do cluster" não é, por si só, um controle de acesso. Em um deploy Kubernetes
  real, isso deveria ser reforçado adicionalmente por uma NetworkPolicy restringindo quem
  pode alcançar o serviço, ou simplesmente não expondo esta rota pelo Ingress
