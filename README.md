# Conexão Solidária

MVP da plataforma de gestão de doações da ONG Esperança Solidária — Hackathon Pós-Tech.

## Documentação

- [BRD](docs/brd.md)
- [Documento de Arquitetura](docs/architecture.md)
- [ADRs](docs/adr/)

## Como rodar localmente — Api.CampanhasUsuarios

Pré-requisitos: .NET 8 SDK, Docker.

```bash
docker compose up -d postgres-campanhas rabbitmq
dotnet run --project src/Api.CampanhasUsuarios
```

A API sobe na porta exibida no console (Swagger disponível em `/swagger` no ambiente de
desenvolvimento). Endpoints principais:

- `POST /doadores` — cadastro público de doador
- `POST /auth/login` — autenticação (GestorONG já seedado: `gestor@esperancasolidaria.org` / `Admin123!`)
- `POST /campanhas`, `PUT /campanhas/{id}`, `PATCH /campanhas/{id}/status` — gestão de campanhas (GestorONG)
- `GET /campanhas` — painel público de transparência (apenas campanhas ativas)
- `POST /doacoes` — intenção de doação (Doador logado) — publica evento no RabbitMQ, não escreve o total diretamente
- `PATCH /internal/campanhas/{id}/arrecadado` — uso interno do `Worker.Doacoes`
- `GET /health` — healthcheck

## Rodar os testes

```bash
dotnet test tests/Api.CampanhasUsuarios.Tests
```

Os testes usam EF Core InMemory e um publicador de eventos fake — não exigem Postgres/RabbitMQ
rodando.

## Como rodar localmente — Worker.Doacoes

Pré-requisitos: .NET 8 SDK, Docker, e a `Api.CampanhasUsuarios` já rodando (o Worker depende
dela para o callback de atualização de saldo).

```bash
docker compose up -d postgres-doacoes rabbitmq
dotnet run --project src/Worker.Doacoes
```

Antes de rodar, confira que `src/Worker.Doacoes/appsettings.json` aponta `CampanhasApi:BaseUrl`
para a porta real em que a `Api.CampanhasUsuarios` está rodando, e que `CampanhasApi:SharedSecret`
é idêntico ao `Internal:SharedSecret` configurado em `src/Api.CampanhasUsuarios/appsettings.json`
— caso contrário, todo callback será rejeitado com 401.

O Worker não expõe endpoints além de `GET /health` — ele consome a fila `doacao-recebida` do
RabbitMQ, chama a API para atualizar o total arrecadado da campanha e, só após a chamada ter
sucesso, persiste um ledger de doações processadas em seu próprio banco (`DoacoesDb`). Essa
ordem é proposital: o registro no ledger funciona como marcador de conclusão, garantindo que uma
reentrega da mensagem tente a chamada novamente em vez de pular um callback que falhou.

## Rodar os testes do Worker.Doacoes

```bash
dotnet test tests/Worker.Doacoes.Tests
```

Assim como nos testes da API, nenhum teste automatizado depende de RabbitMQ ou Postgres reais.

## Testar o fluxo completo manualmente (ponta a ponta)

Com ambos os bancos e o RabbitMQ no ar (`docker compose up -d`) e os dois serviços rodando:

1. Autentique-se como GestorONG (`POST /auth/login`), crie uma campanha (`POST /campanhas`).
2. Cadastre e autentique-se como Doador (`POST /doadores`, depois `POST /auth/login`).
3. Envie uma doação (`POST /doacoes`).
4. Abra a interface de gestão do RabbitMQ (`http://localhost:15672`, usuário/senha `guest`) e
   observe a mensagem passando pela fila `doacao-recebida`.
5. Consulte `GET /campanhas` — o `ValorArrecadado` deve refletir a doação após o Worker
   processar a mensagem.

> Para o design planejado da observabilidade (Zabbix/Grafana), ver
> [ADR-005](docs/adr/ADR-005-observabilidade-zabbix-grafana.md) e `docs/architecture.md`. A
> infraestrutura de Kubernetes, o pipeline de CI/CD e a observabilidade já estão implementados
> — ver as seções abaixo.

## Rodar em Kubernetes (Minikube)

Pré-requisitos: Docker, `kubectl`, Minikube, e as imagens dos três serviços buildadas
localmente:

```bash
docker build -f src/Api.CampanhasUsuarios/Dockerfile -t api-campanhas-usuarios:local .
docker build -f src/Worker.Doacoes/Dockerfile -t worker-doacoes:local .
docker build -f src/Frontend.ConexaoSolidaria/Dockerfile -t frontend-conexao-solidaria:local .
```

```bash
minikube start
minikube addons enable metrics-server
minikube image load api-campanhas-usuarios:local
minikube image load worker-doacoes:local
minikube image load frontend-conexao-solidaria:local
kubectl apply -k k8s/overlays/local/
kubectl get pods --watch
```

Aguarde os 8 pods (`api-campanhas-usuarios`, `worker-doacoes`, `frontend`, `postgres-campanhas`,
`postgres-doacoes`, `rabbitmq`, `zabbix`, `grafana`) ficarem `1/1 Running`. Depois:

```bash
minikube service api-campanhas-usuarios --url
minikube service frontend --url
```

A primeira URL é a API (fluxo de ponta a ponta via `curl`/Swagger/Scalar, já documentado
acima — agora rodando inteiramente dentro do cluster, não via `dotnet run`). A segunda é o
**frontend** (ver seção própria abaixo) — a interface web completa, já pronta pra uso.

Para reaplicar depois de uma mudança nos manifests: `kubectl apply -k k8s/overlays/local/`.
Para derrubar tudo: `kubectl delete -k k8s/overlays/local/` (ou `minikube delete` para
destruir o cluster inteiro).

## Frontend (Blazor WebAssembly)

Interface web da Conexão Solidária, em `src/Frontend.ConexaoSolidaria` — um projeto Blazor
WebAssembly (mesma solution `.NET` da API/Worker, incluído automaticamente no
`dotnet build`/`dotnet test` do CI). Cobre o fluxo que já era testado manualmente via `curl`:

- **`/`** — lista pública das campanhas ativas, com barra de progresso da meta.
- **`/login`** e **`/cadastro`** — autenticação e cadastro de Doador.
- **`/doar/{campanhaId}`** — fazer uma doação (Doador autenticado).
- **`/campanhas`**, **`/campanhas/nova`**, **`/campanhas/{id}/editar`** — gestão de campanhas
  (GestorONG autenticado).

O container é construído em duas etapas: o SDK do .NET publica os arquivos estáticos do
Blazor, e um `nginx:alpine` final os serve na porta 8080. Como o navegador do usuário não
resolve o DNS interno do cluster, o próprio nginx faz proxy reverso de `/api/*` para o Service
`api-campanhas-usuarios` (configurado em `src/Frontend.ConexaoSolidaria/nginx.conf`) — o
frontend chama `/api/...` (mesma origem), sem CORS e sem precisar saber a URL externa da API.

Acesse pela URL de `minikube service frontend --url` (comando acima). Não há um fluxo de
`dotnet run` local independente documentado aqui — como a comunicação com a API depende desse
proxy do nginx, o Kubernetes (local via Minikube, ou o overlay AWS futuramente) é o ambiente
de execução real deste projeto.

## Configurar Zabbix e Grafana (passo a passo manual)

Zabbix e Grafana sobem automaticamente com o comando acima, mas a configuração de
monitoramento em si (quais hosts/itens monitorar, o dashboard) é feita manualmente pela
interface web — não há uma forma limpa de declarar isso via manifest sem usar a API do
Zabbix, o que ficou fora do escopo deste MVP.

**1. Acessar o Zabbix:**

```bash
minikube service zabbix --url
```

Login padrão da imagem `zabbix-appliance`: usuário `Admin`, senha `zabbix`.

**2. Criar um host e um item de monitoramento para cada serviço** (repita para
`api-campanhas-usuarios` e `worker-doacoes`):

- Menu **Configuration → Hosts → Create host**
  - Host name: `api-campanhas-usuarios` (ou `worker-doacoes`)
  - Host groups: crie um grupo novo, ex. `Conexao Solidaria`
- Na aba **Items** do host recém-criado, **Create item**:
  - Name: `Health Check`
  - Type: `HTTP agent`
  - Key: `health.check`
  - URL: `http://api-campanhas-usuarios/health` (ou `http://worker-doacoes/health`)
  - Update interval: `30s`

O tipo `HTTP agent` faz o próprio servidor Zabbix chamar a URL periodicamente — não exige
instalar um agente Zabbix dentro dos containers da aplicação.

**3. Acessar o Grafana:**

```bash
minikube service grafana --url
```

Login padrão: usuário `admin`, senha `admin` (ele vai pedir para trocar no primeiro acesso).

**4. Conectar o Grafana ao Zabbix:**

- Menu **Connections → Data sources → Add data source → Zabbix**
- URL: `http://zabbix/api_jsonrpc.php`
- Usuário/senha: os mesmos do passo 1 (`Admin`/`zabbix`)
- Salvar e testar a conexão

**5. Criar o dashboard:**

- **Dashboards → New → New dashboard → Add visualization**
- Selecione a datasource Zabbix criada acima
- Escolha o grupo de host `Conexao Solidaria`, os hosts e o item `Health Check` criados no
  passo 2
- Salve o dashboard — esse é o painel a ser mostrado no vídeo de demonstração exigido pelo
  enunciado, com métricas reais dos serviços rodando no cluster.

## Ambiente AWS (complementar)

A aplicação também roda publicamente na AWS, provisionada via Terraform em um repositório
separado (ver [ADR-007](docs/adr/ADR-007-infraestrutura-aws-terraform.md)). O overlay
Kubernetes usado nesse ambiente (`k8s/overlays/aws/`) está documentado em
`k8s/overlays/aws/README.md` — este repositório não provisiona nem aplica esse overlay
sozinho, apenas fornece a estrutura que o repositório de infraestrutura consome.
