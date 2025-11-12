# get-secrets.ps1

# Ignore SSL certificate validation
if ($PSVersionTable.PSVersion.Major -ge 6) {
    # PowerShell Core (6+)
    $PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
}
else {
    # Windows PowerShell (5.1 and earlier)
    add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
}

# Load admin credentials from .env file
$envContent = Get-Content -Path "$PSScriptRoot/.env.docker"
$envVars = @{}

foreach ($line in $envContent) {
    if ($line -match '^\s*([^#][^=]+)=(.*)$') {
        $key = $matches[1].Trim()
        $value = $matches[2].Trim()
        $envVars[$key] = $value
        # Also set as environment variable for current session
        Set-Item -Path "env:$key" -Value $value
    }
}

$keycloakUrl = $envVars["KC_URL"]
$realm = $envVars["KC_REALM"]
$adminUser = $envVars["KEYCLOAK_ADMIN"]
$adminPassword = $envVars["KEYCLOAK_ADMIN_PASSWORD"]


# Get admin token
$tokenResponse = Invoke-RestMethod -Method Post `
    -Uri "$keycloakUrl/realms/master/protocol/openid-connect/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
    client_id  = "admin-cli"
    username   = $adminUser
    password   = $adminPassword
    grant_type = "password"
}

$token = $tokenResponse.access_token

$realmConfig = Get-Content -Path "$PSScriptRoot/data/realm.json" -Raw | ConvertFrom-Json

# Extract client IDs where clientAuthenticatorType is "client-secret"
$services = $realmConfig.clients | 
Where-Object { 
    $_.clientAuthenticatorType -eq "client-secret" -and 
    -not $_.name.Contains("$") 
} | 
Select-Object -ExpandProperty clientId

foreach ($service in $services) {
    # Get client ID
    $clientResponse = Invoke-RestMethod -Method Get `
        -Uri "$keycloakUrl/admin/realms/$realm/clients?clientId=$service" `
        -Headers @{
        Authorization = "Bearer $token"
    }
    
    $clientId = $clientResponse[0].id
    
    # Get client secret
    $secretResponse = Invoke-RestMethod -Method Post `
        -Uri "$keycloakUrl/admin/realms/$realm/clients/$clientId/client-secret" `
        -Headers @{
        Authorization = "Bearer $token"
    }
    
    $secret = $secretResponse.value
    
    # Append to secrets file
    $secret | Out-File -FilePath "$PSScriptRoot/${service}.secret.txt" -NoNewline
}

Write-Host "Secrets extracted to .secret.txt files."
