<#
.SYNOPSIS
  Cria roles, policies e permissions em instâncias Directus >= v11 usando credenciais de administrador ou token de API.

.DESCRIPTION
  O modelo RBAC do Directus v11 exige que as permissions pertençam a uma policy, que por sua vez referencia uma ou mais roles.
  Este script automatiza:
    1. Autenticação opcional via token de API ou email/password de administrador.
    2. Criação de roles.
    3. Criação de policies associadas a cada role.
    4. Criação de permissions ligadas a cada policy.

.PARAMETER BaseUrl
  URL base da API Directus (ex.: https://meu-directus.example.com)

.PARAMETER AdminEmail
  (Opcional) Email do utilizador administrador no Directus.

.PARAMETER AdminPass
  (Opcional) Password desse utilizador administrador.

.PARAMETER ApiToken
  (Opcional) Token de API JWT já existente. Se fornecido, o script não faz login.
#>
param(
  [Parameter(Mandatory=$true)] [string]$BaseUrl,
  [Parameter(Mandatory=$false)] [string]$AdminEmail,
  [Parameter(Mandatory=$false)] [string]$AdminPass,
  [Parameter(Mandatory=$false)] [string]$ApiToken
)

# 1. Autenticação ou uso de token existente
if ($ApiToken) {
    Write-Host "→ Usando token de API fornecido."
    $Token = $ApiToken
} else {
    if (-not $AdminEmail -or -not $AdminPass) {
        throw "Forneça um ApiToken (-ApiToken) ou credenciais (-AdminEmail e -AdminPass)."
    }
    Write-Host "→ A autenticar $AdminEmail..."
    $authBody = @{ email = $AdminEmail; password = $AdminPass } | ConvertTo-Json
    try {
        $authResp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/auth/login" -Headers @{ 'Content-Type' = 'application/json' } -Body $authBody -ErrorAction Stop
        $Token = $authResp.data.access_token
        Write-Host "✅ Autenticado com sucesso."
    } catch {
        Write-Host "❌ Falha na autenticação."
        if ($_.Exception.Response -and $_.Exception.Response.Content) {
            try {
                $detail = $_.Exception.Response.Content | ConvertFrom-Json | ConvertTo-Json -Depth 5
                Write-Host "Detalhes: $detail"
            } catch {
                Write-Host "Resposta inválida: $($_.Exception.Response.Content)"
            }
        } else {
            Write-Host $_.Exception.Message
        }
        exit 1
    }
}

$Headers = @{ Authorization = "Bearer $Token"; 'Content-Type' = 'application/json' }

# 2. Funções auxiliares com logging de erros
function New-Role {
  param(
    [string]$Name,
    [string]$Description
  )
  Write-Host "→ Criar role '$Name'..."
  $body = @{ name = $Name; description = $Description } | ConvertTo-Json
  try {
    $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/roles" -Headers $Headers -Body $body -ErrorAction Stop
    Write-Host "✅ Role '$Name' criada (ID: $($resp.data.id))."
    return $resp.data.id
  } catch {
    Write-Host "❌ Falha ao criar role '$Name'!"
    if ($_.Exception.Response -and $_.Exception.Response.Content) {
      try { $err = $_.Exception.Response.Content | ConvertFrom-Json; Write-Host "Response: $( $err | ConvertTo-Json -Depth 5 )" } catch { Write-Host "Resposta inválida: $($_.Exception.Response.Content)" }
    } elseif ($_.Exception.Response) {
      Write-Host "Status Code: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)"
    } else {
      Write-Host $_.Exception.Message
    }
    exit 1
  }
}

function New-Policy {
  param(
    [string]$Name,
    [string]$RoleId
  )
  Write-Host "→ Criar policy '$Name'..."
  $body = @{ name = "$Name policy"; icon = 'rule'; roles = @($RoleId); description = "Policy para $Name" } | ConvertTo-Json
  try {
    $resp = Invoke-RestMethod -Method Post -Uri "$BaseUrl/policies" -Headers $Headers -Body $body -ErrorAction Stop
    Write-Host "✅ Policy '$Name' criada (ID: $($resp.data.id))."
    return $resp.data.id
  } catch {
    Write-Host "❌ Falha ao criar policy '$Name'!"
    if ($_.Exception.Response -and $_.Exception.Response.Content) {
      try { $err = $_.Exception.Response.Content | ConvertFrom-Json; Write-Host "Response: $( $err | ConvertTo-Json -Depth 5 )" } catch { Write-Host "Resposta inválida: $($_.Exception.Response.Content)" }
    } elseif ($_.Exception.Response) {
      Write-Host "Status Code: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)"
    } else {
      Write-Host $_.Exception.Message
    }
    exit 1
  }
}

function New-Permission {
  param(
    [string]$PolicyId,
    [string]$Collection,
    [string]$Action,
    [array] $Fields       = @(),
    [hashtable]$Filter    = @{}
  )
  Write-Host "→ Criar permission '$Action' em '$Collection' para policy $PolicyId..."
  $perm = @{ policy = $PolicyId; collection = $Collection; action = $Action }
  if ($Fields.Count -gt 0)   { $perm.fields      = $Fields }
  if ($Filter.Count -gt 0)   { $perm.permissions = $Filter }
  $body = $perm | ConvertTo-Json -Depth 10
  try {
    Invoke-RestMethod -Method Post -Uri "$BaseUrl/permissions" -Headers $Headers -Body $body -ErrorAction Stop
    Write-Host "✅ Permission '$Action' em '$Collection' criada."
  } catch {
    Write-Host "❌ Falha ao criar permission '$Action' em '$Collection'!"
    if ($_.Exception.Response -and $_.Exception.Response.Content) {
      try { $err = $_.Exception.Response.Content | ConvertFrom-Json; Write-Host "Response: $( $err | ConvertTo-Json -Depth 5 )" } catch { Write-Host "Resposta inválida: $($_.Exception.Response.Content)" }
    } elseif ($_.Exception.Response) {
      Write-Host "Status Code: $($_.Exception.Response.StatusCode) - $($_.Exception.Response.StatusDescription)"
    } else {
      Write-Host $_.Exception.Message
    }
    exit 1
  }
}

# 3. Execução principal
Write-Host "===== Início da configuração RBAC ====="

# Criar roles
$PresidenteId   = New-Role -Name 'presidente'   -Description 'Total exceto configurações'
$TeamLeaderId   = New-Role -Name 'team_leader'  -Description 'Gestão de publicações da equipa'
$OrientadorId   = New-Role -Name 'orientador'   -Description 'Publicações dos seus orientandos'
$BolseiroId     = New-Role -Name 'bolseiro'     -Description 'Acesso limitado a dados próprios'
$TecnicoId      = New-Role -Name 'tecnico'      -Description 'Acesso a funcionalidades técnicas específicas'

# Criar policies
$PresidentePol  = New-Policy -Name 'presidente'   -RoleId $PresidenteId
$TeamLeaderPol  = New-Policy -Name 'team_leader'  -RoleId $TeamLeaderId
$OrientadorPol  = New-Policy -Name 'orientador'   -RoleId $OrientadorId
$BolseiroPol    = New-Policy -Name 'bolseiro'     -RoleId $BolseiroId
$TecnicoPol     = New-Policy -Name 'tecnico'      -RoleId $TecnicoId

# Permissões gerais para presidente
foreach ($action in 'create','read','update','delete','share') {
  New-Permission -PolicyId $PresidentePol -Collection '*'             -Action $action
}

# Permissões restritas para team_leader
$teamFilter = @{ team = @{ _eq = '$CURRENT_USER.team' } }
foreach ($action in 'create','read','update','delete') {
  New-Permission -PolicyId $TeamLeaderPol -Collection 'publications' -Action $action -Filter $teamFilter
}

# Permissões restritas para orientador
$orientFilter = @{ 'persons_publications.person_id.orientador' = @{ _eq = '$CURRENT_USER.id' } }
foreach ($action in 'read','update','delete') {
  New-Permission -PolicyId $OrientadorPol -Collection 'publications' -Action $action -Filter $orientFilter
}

# Permissões para bolseiro
$bolseiroFilter = @{ owner_id = @{ _eq = '$CURRENT_USER.id' } }
foreach ($action in 'create','read','update','delete') {
  New-Permission -PolicyId $BolseiroPol -Collection 'publications' -Action $action -Filter $bolseiroFilter
}

# Permissões para tecnico
foreach ($action in 'read','update') {
  New-Permission -PolicyId $TecnicoPol -Collection 'system'       -Action $action
  New-Permission -PolicyId $TecnicoPol -Collection 'settings'     -Action $action
}

Write-Host "===== Configuração concluída com sucesso! ====="
# End of script
