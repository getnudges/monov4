#!/usr/bin/env pwsh

function New-RandomString {
    param (
        [int]$length = 32
    )
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    return -join ((1..$length) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
}

function New-EnvFile {
    param (
        [string]$templatePath,
        [string]$dbPassword,
        [string]$apiKey
    )
    
    $templateContent = Get-Content $templatePath -Raw
    $outputPath = $templatePath -replace '\.template$', '.docker'
    
    # Skip if .env.docker already exists
    if (Test-Path $outputPath) {
        Write-Host "Skipping existing file: $outputPath"
        return
    }

    Write-Host "Generating $outputPath from template..."

    $adminUsername = 'admin'
    $adminPassword = (New-RandomString -length 16)
    $placeholders = @{
        # Keycloak admin credentials
        'ADMIN_USERNAME'              = $adminUsername
        'ADMIN_PASSWORD'              = $adminPassword
        'KC_DB_USERNAME'              = $adminUsername
        'KC_DB_PASSWORD'              = $dbPassword
        'KC_BOOTSTRAP_ADMIN_USERNAME' = $adminUsername
        'KC_BOOTSTRAP_ADMIN_PASSWORD' = $adminPassword

        # Database credentials (all use the same value)
        'DB_PASSWORD'                 = $dbPassword
        'USERDB_PASSWORD'             = $dbPassword
        'PRODUCTDB_PASSWORD'          = $dbPassword
        'PAYMENTDB_PASSWORD'          = $dbPassword

        # API keys and secrets
        'API_KEY'                     = $apiKey
        'API_TOKEN'                   = (New-RandomString -length 32)
        'AUTH_API_KEY'                = (New-RandomString -length 32)

        # Stripe configuration (using test mode values)
        'STRIPE_API_KEY'              = 'sk_test_' + (New-RandomString -length 32)
        'STRIPE_WEBHOOKS_SECRET'      = 'whsec_' + (New-RandomString -length 32)

        # Twilio configuration (placeholder values)
        'TWILIO_ACCOUNT_SID'          = 'AC' + (New-RandomString -length 32)
        'TWILIO_AUTH_TOKEN'           = (New-RandomString -length 32)
        'TWILIO_MESSAGE_SERVICE_SID'  = 'MG' + (New-RandomString -length 32)

        # Unleash tokens
        'UNLEASH_FRONTEND_TOKEN'      = 'development.' + (New-RandomString -length 16)
        'UNLEASH_CLIENT_TOKEN'        = 'development.' + (New-RandomString -length 16)

        # OIDC credentials (match Keycloak admin)
        'OIDC_ADMIN_USERNAME'         = 'admin'
        'OIDC_ADMIN_PASSWORD'         = (New-RandomString -length 16)
    }
    
    $newContent = $templateContent
    foreach ($key in $placeholders.Keys) {
        $placeholder = "{{$key}}"
        $value = $placeholders[$key]
        $newContent = $newContent -replace [regex]::Escape($placeholder), $value
    }
    
    Set-Content -Path $outputPath -Value $newContent
    Write-Host "Generated $outputPath successfully"
}
    
# Generate a single DB password for all DB-related placeholders
$dbPassword = (New-RandomString -length 16)
$apiKey = (New-RandomString -length 32)

# Find all .env.template files and generate corresponding .env.docker files
Get-ChildItem -Path . -Filter ".env.template" -Recurse | ForEach-Object {
    New-EnvFile -templatePath $_.FullName -dbPassword $dbPassword -apiKey $apiKey
}

Write-Host "Environment files generation complete!"

# Generate headers file from template if present
$headersTemplate = Join-Path -Path "." -ChildPath "dotnet/GraphMonitor/headers.template"
$headersOutput = Join-Path -Path "." -ChildPath "dotnet/GraphMonitor/headers"
if (Test-Path $headersTemplate) {
    if (-not (Test-Path $headersOutput)) {
        Write-Host "Generating $headersOutput from template..."
        $headersContent = Get-Content $headersTemplate -Raw
        $headersContent = [regex]::Replace($headersContent, '\{\{\s*API_KEY\s*\}\}', [System.Text.RegularExpressions.MatchEvaluator] { param($m) return $apiKey })

        Set-Content -Path $headersOutput -Value $headersContent
        Write-Host "Generated $headersOutput successfully"
    }
    else {
        Write-Host "Skipping existing file: $headersOutput"
    }
}
