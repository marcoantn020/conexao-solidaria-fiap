# k8s/overlays/aws/README.md

Este overlay não é aplicado por este repositório. Ele existe como o ponto de extensão que o
repositório de infraestrutura Terraform (ver ADR-007 em `docs/adr/ADR-007-infraestrutura-aws-terraform.md`
deste repo) usa para publicar a aplicação na AWS.

## O que o Terraform precisa substituir antes de aplicar este overlay

1. **Imagens** (`kustomization.yaml`, seção `images:`): trocar `SUBSTITUIR-PELO-TERRAFORM` pela
   URL real do ECR criado pelo Terraform, e `SUBSTITUIR-PELA-TAG-DA-IMAGEM` pela tag da imagem
   publicada pelo pipeline de CI deste repositório (ex.: o SHA do commit).
2. **Connection strings do RDS** (`rds-connection-patch-campanhas.yaml` e `rds-connection-patch-doacoes.yaml`):
   trocar `<rds-endpoint-output-do-terraform>`, `<usuario>` e `<senha-do-secrets-manager>` pelos
   valores reais gerados pelo `terraform apply` (via `terraform output` ou lidos diretamente do
   AWS Secrets Manager, dependendo de como o Terraform for desenhado). O patch é dividido em dois
   arquivos porque o Kustomize rejeita um patch estratégico multi-documento combinado com uma única
   entrada `target:` que usa regex (`|`) no nome — cada Secret precisa do seu próprio `path` e `target`
   em `kustomization.yaml`.
3. **Segredos de aplicação** (`k8s/base/api-campanhas-usuarios/secret.yaml` e
   `k8s/base/worker-doacoes/secret.yaml`): trocar `Jwt__Key`, `Internal__SharedSecret` (API) /
   `CampanhasApi__SharedSecret` (Worker) e `RabbitMq__UserName`/`RabbitMq__Password` pelos valores
   reais gerados pelo Terraform (ex.: AWS Secrets Manager) — os valores atuais são placeholders de
   desenvolvimento e este overlay promove o Service da API para `LoadBalancer` (publicamente
   acessível), então aplicá-lo sem substituir esses segredos expõe credenciais de dev na internet.
   `Internal__SharedSecret` (API) e `CampanhasApi__SharedSecret` (Worker) precisam continuar
   idênticos entre si em qualquer valor real que os substitua, assim como já são no base.

## Como aplicar (depois das substituições acima)

```bash
kubectl apply -k k8s/overlays/aws/
```

Isso assume um `kubectl` já configurado apontando para o cluster k3s provisionado pelo
Terraform (o próprio Terraform normalmente escreve esse kubeconfig como parte do `apply`).
