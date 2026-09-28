<#
.SYNOPSIS
  Popula o ambiente com dados ficticios para a demonstracao da Conexao Solidaria.

.DESCRIPTION
  Usa apenas a API publica, pelo fluxo real: cria campanhas como GestorONG, cadastra
  doadores e envia doacoes que passam pelo RabbitMQ e sao processadas pelo Worker.
  Assim o painel publico, o frontend e o dashboard do Grafana mostram dados coerentes.

  Pode ser executado de novo: campanhas ativas com o mesmo titulo e doadores ja
  cadastrados sao reaproveitados, sem duplicar doacoes.

.EXAMPLE
  kubectl port-forward svc/api-campanhas-usuarios 8080:80
  powershell -ExecutionPolicy Bypass -File scripts/popular-dados-demo.ps1
#>
param(
    [string]$ApiUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

function Invoke-Api {
    param([string]$Method, [string]$Path, $Body, [string]$Token)
    $headers = @{}
    if ($Token) { $headers["Authorization"] = "Bearer $Token" }
    $params = @{ Method = $Method; Uri = "$ApiUrl$Path"; Headers = $headers; ContentType = "application/json; charset=utf-8" }
    if ($null -ne $Body) {
        $params["Body"] = [System.Text.Encoding]::UTF8.GetBytes(($Body | ConvertTo-Json -Depth 5))
    }
    # Write-Output enumera arrays JSON (no PowerShell 5.1 eles chegam como um unico objeto)
    Invoke-RestMethod @params | Write-Output
}

function Get-Token([string]$Email, [string]$Senha) {
    (Invoke-Api -Method Post -Path "/auth/login" -Body @{ email = $Email; senha = $Senha }).token
}

$campanhas = @(
    @{ titulo = "Natal Solidário: brinquedos para 120 crianças"; descricao = "Compra de brinquedos e kits de Natal para todas as crianças acolhidas."; dataFim = "2026-12-20T23:59:59Z"; meta = 12000; alvo = 0.82; status = "Ativa" },
    @{ titulo = "Cestas básicas para famílias acolhidas"; descricao = "Cestas mensais para 40 famílias acompanhadas pela ONG."; dataFim = "2026-11-30T23:59:59Z"; meta = 6000; alvo = 0.97; status = "Ativa" },
    @{ titulo = "Material escolar 2027"; descricao = "Mochila, cadernos e estojo completo para o próximo ano letivo."; dataFim = "2027-02-15T23:59:59Z"; meta = 8000; alvo = 0.64; status = "Ativa" },
    @{ titulo = "Reforma da brinquedoteca"; descricao = "Pintura, piso emborrachado e novos móveis para a brinquedoteca."; dataFim = "2027-03-31T23:59:59Z"; meta = 25000; alvo = 0.26; status = "Ativa" },
    @{ titulo = "Aulas de reforço escolar"; descricao = "Um semestre de aulas de reforço em português e matemática."; dataFim = "2027-06-30T23:59:59Z"; meta = 15000; alvo = 0.15; status = "Ativa" },
    @{ titulo = "Uniformes para o time de futsal"; descricao = "Uniformes e chuteiras para as 18 crianças do time da ONG."; dataFim = "2026-12-10T23:59:59Z"; meta = 3500; alvo = 0.45; status = "Ativa" },
    @{ titulo = "Campanha do Agasalho 2026"; descricao = "Roupas de frio e cobertores para o inverno."; dataFim = "2026-12-31T23:59:59Z"; meta = 4000; alvo = 1.0; status = "Concluida" },
    @{ titulo = "Passeio ao zoológico"; descricao = "Transporte e ingressos para um passeio com as crianças."; dataFim = "2026-12-31T23:59:59Z"; meta = 2500; alvo = 0.2; status = "Cancelada" }
)

$doadores = @(
    @{ nomeCompleto = "Ana Paula Ferreira"; email = "ana.ferreira@example.com"; cpf = "104.332.181-00" },
    @{ nomeCompleto = "Bruno Carvalho Lima"; email = "bruno.lima@example.com"; cpf = "960.013.389-14" },
    @{ nomeCompleto = "Camila Rocha Santos"; email = "camila.santos@example.com"; cpf = "083.863.794-99" },
    @{ nomeCompleto = "Diego Martins Alves"; email = "diego.alves@example.com"; cpf = "026.542.351-14" },
    @{ nomeCompleto = "Eduarda Nogueira Costa"; email = "eduarda.costa@example.com"; cpf = "161.559.407-89" },
    @{ nomeCompleto = "Felipe Araújo Ribeiro"; email = "felipe.ribeiro@example.com"; cpf = "816.184.959-50" },
    @{ nomeCompleto = "Gabriela Mendes Pires"; email = "gabriela.pires@example.com"; cpf = "310.341.316-56" },
    @{ nomeCompleto = "Henrique Souza Barros"; email = "henrique.barros@example.com"; cpf = "475.255.341-44" },
    @{ nomeCompleto = "Isabela Duarte Melo"; email = "isabela.melo@example.com"; cpf = "928.327.648-51" },
    @{ nomeCompleto = "João Pedro Teixeira"; email = "joao.teixeira@example.com"; cpf = "350.305.641-60" }
)
$senhaDoadores = "Senha123!"
$valoresDoacao = @(25, 30, 50, 50, 80, 100, 100, 120, 150, 200, 250, 300, 500)

Write-Host "Autenticando como GestorONG..."
$tokenGestor = Get-Token "gestor@esperancasolidaria.org" "Admin123!"

Write-Host "Cadastrando doadores..."
$tokensDoadores = @()
foreach ($d in $doadores) {
    try {
        Invoke-Api -Method Post -Path "/doadores" -Body @{ nomeCompleto = $d.nomeCompleto; email = $d.email; cpf = $d.cpf; senha = $senhaDoadores } | Out-Null
        Write-Host "  + $($d.nomeCompleto)"
    } catch {
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 409) { Write-Host "  = $($d.nomeCompleto) (ja cadastrado)" } else { throw }
    }
    $tokensDoadores += Get-Token $d.email $senhaDoadores
}

$ativasExistentes = @(Invoke-Api -Method Get -Path "/campanhas" | ForEach-Object { $_.titulo })
# Campanhas encerradas nao aparecem no painel; se a carga ja rodou antes, nao as recria.
$jaPopulado = $ativasExistentes -contains $campanhas[0].titulo
$random = New-Object System.Random 2026
$esperado = @{}
$total = 0

foreach ($c in $campanhas) {
    if ($ativasExistentes -contains $c.titulo -or ($c.status -ne "Ativa" -and $jaPopulado)) {
        Write-Host "Campanha ja existe, mantida: $($c.titulo)"
        continue
    }

    $criada = Invoke-Api -Method Post -Path "/campanhas" -Token $tokenGestor -Body @{
        titulo = $c.titulo; descricao = $c.descricao; dataInicio = "2026-09-01T00:00:00Z"; dataFim = $c.dataFim; metaFinanceira = $c.meta
    }

    $objetivo = [decimal]$c.meta * [decimal]$c.alvo
    $soma = [decimal]0
    $qtd = 0
    while ($soma -lt $objetivo) {
        $valor = [decimal]$valoresDoacao[$random.Next($valoresDoacao.Count)]
        if ($soma + $valor -gt $objetivo -and $objetivo - $soma -ge 10) { $valor = [decimal]([math]::Floor($objetivo - $soma)) }
        $token = $tokensDoadores[$random.Next($tokensDoadores.Count)]
        Invoke-Api -Method Post -Path "/doacoes" -Token $token -Body @{ idCampanha = $criada.id; valorDoacao = $valor } | Out-Null
        $soma += $valor
        $qtd++
        Start-Sleep -Milliseconds 120
    }
    $total += $qtd

    if ($c.status -ne "Ativa") {
        Invoke-Api -Method Patch -Path "/campanhas/$($criada.id)/status" -Token $tokenGestor -Body @{ status = $c.status } | Out-Null
    } else {
        $esperado[$criada.id] = $soma
    }
    Write-Host ("  + {0}: {1} doacoes, R$ {2:N2} ({3})" -f $c.titulo, $qtd, $soma, $c.status)
}

Write-Host "`n$total doacoes enviadas para a fila. Aguardando o Worker processar..."
$limite = (Get-Date).AddSeconds(90)
do {
    Start-Sleep -Seconds 3
    $painel = @(Invoke-Api -Method Get -Path "/campanhas")
    $pendentes = @($esperado.Keys | Where-Object { $id = $_; $c = $painel | Where-Object { $_.id -eq $id }; -not $c -or [decimal]$c.valorArrecadado -lt $esperado[$id] })
} while ($pendentes.Count -gt 0 -and (Get-Date) -lt $limite)

if ($pendentes.Count -gt 0) {
    Write-Warning "$($pendentes.Count) campanha(s) ainda nao refletem todas as doacoes. Confira: kubectl logs deploy/worker-doacoes"
}

Write-Host "`nPainel publico (GET /campanhas):"
$painel | Sort-Object { [decimal]$_.valorArrecadado / [decimal]$_.metaFinanceira } -Descending |
    Select-Object titulo,
        @{ n = "meta"; e = { "R$ {0:N2}" -f $_.metaFinanceira } },
        @{ n = "arrecadado"; e = { "R$ {0:N2}" -f $_.valorArrecadado } },
        @{ n = "%"; e = { "{0:N0}%" -f (100 * $_.valorArrecadado / $_.metaFinanceira) } } |
    Format-Table -AutoSize

Write-Host "Doadores de exemplo: senha '$senhaDoadores' (ex.: ana.ferreira@example.com)"
