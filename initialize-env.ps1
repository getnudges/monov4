#!/usr/bin/env pwsh

function New-RandomString {
    param (
        [int]$length = 32
    )
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    return -join ((1..$length) | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
}

function Read-EnvMaster {
    param (
        [string]$path = ".env.master"
    )
    
    $envVars = @{}
    if (Test-Path $path) {
        Get-Content $path | ForEach-Object {
            $line = $_.Trim()
            if ($line -and !$line.StartsWith('#')) {
                if ($line -match '^([^=]+)=(.*)$') {
                    $key = $matches[1].Trim()
                    $value = $matches[2].Trim().Trim('"')
                    $envVars[$key] = $value
                }
            }
        }
    }
    return $envVars
}

function Write-EnvMaster {
    param (
        [hashtable]$values,
        [string]$path = ".env.master"
    )
    
    $content = @"
# Nudges Master Environment Configuration
# This file is the single source of truth for all environment configurations
# Generated: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")

# Database Credentials
DB_PASSWORD=$($values['DB_PASSWORD'])

# Admin Credentials
ADMIN_USERNAME=$($values['ADMIN_USERNAME'])
ADMIN_PASSWORD=$($values['ADMIN_PASSWORD'])

# API Keys
API_KEY=$($values['API_KEY'])
AUTH_API_KEY=$($values['AUTH_API_KEY'])

# Stripe Configuration
STRIPE_API_KEY=$($values['STRIPE_API_KEY'])
STRIPE_WEBHOOKS_SECRET=$($values['STRIPE_WEBHOOKS_SECRET'])

# Twilio Configuration
TWILIO_ACCOUNT_SID=$($values['TWILIO_ACCOUNT_SID'])
TWILIO_AUTH_TOKEN=$($values['TWILIO_AUTH_TOKEN'])
TWILIO_MESSAGE_SERVICE_SID=$($values['TWILIO_MESSAGE_SERVICE_SID'])

# Unleash Tokens
UNLEASH_FRONTEND_TOKEN=$($values['UNLEASH_FRONTEND_TOKEN'])
UNLEASH_CLIENT_TOKEN=$($values['UNLEASH_CLIENT_TOKEN'])

# OIDC Credentials
OIDC_ADMIN_USERNAME=$($values['OIDC_ADMIN_USERNAME'])
OIDC_ADMIN_PASSWORD=$($values['OIDC_ADMIN_PASSWORD'])

# OIDC Configuration
Oidc__Realm=$($values['Oidc__Realm'])
Oidc__ServerUrl=$($values['Oidc__ServerUrl'])
OIDC_SERVER_AUTH_URL=$($values['OIDC_SERVER_AUTH_URL'])
Oidc__AdminCredentials__AdminClientId=$($values['Oidc__AdminCredentials__AdminClientId'])
Oidc__AdminCredentials__Username=$($values['Oidc__AdminCredentials__Username'])
Oidc__AdminCredentials__Password=$($values['Oidc__AdminCredentials__Password'])

# Security Settings
IGNORE_SSL_CERT_VALIDATION=$($values['IGNORE_SSL_CERT_VALIDATION'])

# Authentication Settings
Authentication__Schemes__Bearer__RequireHttpsMetadata=$($values['Authentication__Schemes__Bearer__RequireHttpsMetadata'])
Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer=$($values['Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer'])
Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience=$($values['Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience'])
IdentityModel__Logging=$($values['IdentityModel__Logging'])
"@
    
    Set-Content -Path $path -Value $content
    Write-Host "Generated $path"
}

function New-ConfigFromTemplate {
    param (
        [string]$templatePath,
        [string]$outputPath,
        [hashtable]$values
    )
    
    Write-Host "Generating $outputPath from template..."
    $templateContent = Get-Content $templatePath -Raw
    
    $newContent = $templateContent
    foreach ($key in $values.Keys) {
        $pattern = '\{\{\s*' + [regex]::Escape($key) + '\s*\}\}'
        $value = $values[$key]
        $newContent = [regex]::Replace($newContent, $pattern, [System.Text.RegularExpressions.MatchEvaluator] { param($m) return $value })
    }
    
    Set-Content -Path $outputPath -Value $newContent
    Write-Host "Generated $outputPath successfully"
}

# Check if .env.master exists, if not create it with random values
$masterEnvPath = ".env.master"
$masterValues = Read-EnvMaster -path $masterEnvPath

if ($masterValues.Count -eq 0) {
    Write-Host "No .env.master found, generating new one..."
    
    $dbPassword = New-RandomString -length 16
    $keycloakDbPassword = New-RandomString -length 16
    $keycloakAdminPassword = New-RandomString -length 16

    $masterValues = @{
        # Database credentials
        'DB_PASSWORD'                                                                  = $dbPassword
        
        # Admin credentials
        'ADMIN_USERNAME'                                                               = 'admin'
        'ADMIN_PASSWORD'                                                               = $keycloakAdminPassword
        
        # API keys and secrets
        'API_KEY'                                                                      = (New-RandomString -length 32)
        'AUTH_API_KEY'                                                                 = (New-RandomString -length 32)
        
        # Stripe configuration
        'STRIPE_API_KEY'                                                               = 'sk_test_' + (New-RandomString -length 32)
        'STRIPE_WEBHOOKS_SECRET'                                                       = 'whsec_' + (New-RandomString -length 32)
        
        # Twilio configuration
        'TWILIO_ACCOUNT_SID'                                                           = 'AC' + (New-RandomString -length 32)
        'TWILIO_AUTH_TOKEN'                                                            = (New-RandomString -length 32)
        'TWILIO_MESSAGE_SERVICE_SID'                                                   = 'MG' + (New-RandomString -length 32)
        
        # Unleash tokens
        'UNLEASH_FRONTEND_TOKEN'                                                       = 'development.' + (New-RandomString -length 16)
        'UNLEASH_CLIENT_TOKEN'                                                         = 'development.' + (New-RandomString -length 16)
        
        # OIDC credentials
        'OIDC_ADMIN_USERNAME'                                                          = 'admin'
        'OIDC_ADMIN_PASSWORD'                                                          = $keycloakAdminPassword
        
        # OIDC configuration
        'Oidc__Realm'                                                                  = 'nudges'
        'Oidc__ServerUrl'                                                              = 'https://keycloak:8443'
        'OIDC_SERVER_AUTH_URL'                                                         = 'https://keycloak.local:8443'   
        'Oidc__AdminCredentials__AdminClientId'                                        = 'admin-cli'
        'Oidc__AdminCredentials__Username'                                             = 'admin'
        'Oidc__AdminCredentials__Password'                                             = $keycloakAdminPassword
        
        # Security settings
        'IGNORE_SSL_CERT_VALIDATION'                                                   = 'true'
        
        # Authentication settings
        'Authentication__Schemes__Bearer__RequireHttpsMetadata'                        = 'false'
        'Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer'      = 'https://keyloak.local:8443/realms/nudges'
        'Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience' = 'false'
        'IdentityModel__Logging'                                                       = 'true'
    }
    
    Write-EnvMaster -values $masterValues -path $masterEnvPath
}
else {
    Write-Host "Using existing .env.master"
}

# Build complete placeholders map with derived values
$placeholders = @{
    # Keycloak admin credentials
    'ADMIN_USERNAME'                                                               = $masterValues['ADMIN_USERNAME']
    'ADMIN_PASSWORD'                                                               = $masterValues['ADMIN_PASSWORD']
    'KC_DB_USERNAME'                                                               = 'keycloak'
    'KC_DB_PASSWORD'                                                               = $masterValues['DB_PASSWORD']
    'KC_BOOTSTRAP_ADMIN_USERNAME'                                                  = $masterValues['ADMIN_USERNAME']
    'KC_BOOTSTRAP_ADMIN_PASSWORD'                                                  = $masterValues['ADMIN_PASSWORD']

    # Database credentials (all use the same password)
    'DB_PASSWORD'                                                                  = $masterValues['DB_PASSWORD']
    'USERDB_PASSWORD'                                                              = $masterValues['DB_PASSWORD']
    'PRODUCTDB_PASSWORD'                                                           = $masterValues['DB_PASSWORD']
    'PAYMENTDB_PASSWORD'                                                           = $masterValues['DB_PASSWORD']

    # API keys and secrets
    'API_KEY'                                                                      = $masterValues['API_KEY']
    'AUTH_API_KEY'                                                                 = $masterValues['AUTH_API_KEY']

    # Stripe configuration
    'STRIPE_API_KEY'                                                               = $masterValues['STRIPE_API_KEY']
    'STRIPE_WEBHOOKS_SECRET'                                                       = $masterValues['STRIPE_WEBHOOKS_SECRET']

    # Twilio configuration
    'TWILIO_ACCOUNT_SID'                                                           = $masterValues['TWILIO_ACCOUNT_SID']
    'TWILIO_AUTH_TOKEN'                                                            = $masterValues['TWILIO_AUTH_TOKEN']
    'TWILIO_MESSAGE_SERVICE_SID'                                                   = $masterValues['TWILIO_MESSAGE_SERVICE_SID']

    # Unleash tokens
    'UNLEASH_FRONTEND_TOKEN'                                                       = $masterValues['UNLEASH_FRONTEND_TOKEN']
    'UNLEASH_CLIENT_TOKEN'                                                         = $masterValues['UNLEASH_CLIENT_TOKEN']

    # OIDC credentials
    'OIDC_ADMIN_USERNAME'                                                          = $masterValues['OIDC_ADMIN_USERNAME']
    'OIDC_ADMIN_PASSWORD'                                                          = $masterValues['OIDC_ADMIN_PASSWORD']
    
    # OIDC configuration
    'Oidc__Realm'                                                                  = $masterValues['Oidc__Realm']
    'Oidc__ServerUrl'                                                              = $masterValues['Oidc__ServerUrl']
    'OIDC_SERVER_AUTH_URL'                                                          = $masterValues['OIDC_SERVER_AUTH_URL']
    'Oidc__AdminCredentials__AdminClientId'                                        = $masterValues['Oidc__AdminCredentials__AdminClientId']
    'Oidc__AdminCredentials__Username'                                             = $masterValues['Oidc__AdminCredentials__Username']
    'Oidc__AdminCredentials__Password'                                             = $masterValues['Oidc__AdminCredentials__Password']
    
    # Security settings
    'IGNORE_SSL_CERT_VALIDATION'                                                   = $masterValues['IGNORE_SSL_CERT_VALIDATION']
    
    # Authentication settings
    'Authentication__Schemes__Bearer__RequireHttpsMetadata'                        = $masterValues['Authentication__Schemes__Bearer__RequireHttpsMetadata']
    'Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer'      = $masterValues['Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer']
    'Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience' = $masterValues['Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience']
    'IdentityModel__Logging'                                                       = $masterValues['IdentityModel__Logging']
}

Write-Host "`nRegenerating all configuration files from .env.master...`n"

# Generate all .env.docker files from templates
Get-ChildItem -Path . -Filter ".env.template" -Recurse | ForEach-Object {
    $outputPath = $_.FullName -replace '\.template$', '.docker'
    New-ConfigFromTemplate -templatePath $_.FullName -outputPath $outputPath -values $placeholders
}

# Generate all headers files from templates
Get-ChildItem -Path . -Filter "headers.template" -Recurse | ForEach-Object {
    $outputPath = $_.FullName -replace '\.template$', ''
    New-ConfigFromTemplate -templatePath $_.FullName -outputPath $outputPath -values $placeholders
}

Write-Host "`nConfiguration generation complete!"
Write-Host "All files have been regenerated from .env.master"
