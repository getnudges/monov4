#!/usr/bin/env pwsh
<#
    Nudges Environment Generator

    Modes:
      ./configure.ps1              # same as --docker (legacy default)
      ./configure.ps1 --docker     # generate .env.master + all .env.docker
      ./configure.ps1 --oidc       # generate .env.<service> from Keycloak
      ./configure.ps1 --local      # generate .env.<service>.local + .NET user-secrets
      ./configure.ps1 --refresh    # full regeneration:
                                      - docker envs
                                      - oidc secrets (forced)
                                      - local envs + user-secrets

    PowerShell Core compatible.
#>

[CmdletBinding()]
param(
    [switch]$Docker,
    [switch]$Oidc,
    [switch]$Local,
    [switch]$Refresh
)

# Default mode: docker-only
if (-not ($Docker -or $Oidc -or $Local -or $Refresh)) {
    $Docker = $true
}

# Refresh implies everything
if ($Refresh) {
    $Docker = $true
    $Oidc = $true
    $Local = $true
}

$env:DOTNET_NOLOGO = "1"
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = "false"
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = "false"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

Write-Host "🔧 Nudges configuration generator starting..."

if ($Docker) { Write-Host "   • Docker env generation enabled" }
if ($Oidc)   { Write-Host "   • OIDC secret extraction enabled" }
if ($Local)  { Write-Host "   • Local env + .NET user-secrets enabled" }
if ($Refresh){ Write-Host "   • FULL REGENERATION mode" }

###############################################################################
# Utilities
###############################################################################

function New-RandomString {
    param([int]$length = 32)
    $chars = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
    -join (1..$length | ForEach-Object { $chars[(Get-Random -Maximum $chars.Length)] })
}

function Read-EnvFile {
    param([string]$path)

    $vars = @{}
    if (Test-Path $path) {
        foreach ($line in Get-Content $path) {
            $trim = $line.Trim()
            if ($trim -and !$trim.StartsWith('#') -and $trim -match '^([^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                if ($value.Length -ge 2 -and (
                        ($value.StartsWith('"') -and $value.EndsWith('"')) -or
                        ($value.StartsWith("'") -and $value.EndsWith("'"))
                    )) {
                    $value = $value.Substring(1, $value.Length - 2)
                }
                $vars[$key] = $value
            }
        }
    }
    return $vars
}

function Write-EnvFile {
    param(
        [string]$path,
        [hashtable]$values,
        [string]$headerComment
    )

    $body = ($values.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join "`n"
    $content = "# $headerComment`n# Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n`n$body`n"
    Set-Content -Path $path -Value $content
    Write-Host "✔ Wrote $path"
}

function Render-Template {
    param(
        [string]$templatePath,
        [string]$outputPath,
        [hashtable]$values
    )

    $content = Get-Content $templatePath -Raw
    foreach ($key in $values.Keys) {
        $pattern = '\{\{\s*' + [regex]::Escape($key) + '\s*\}\}'
        $replacement = $values[$key]
        $content = [regex]::Replace($content, $pattern, $replacement)
    }
    Set-Content -Path $outputPath -Value $content
    Write-Host "✔ Generated $outputPath"
}

###############################################################################
# .env.master creation and loading
###############################################################################

$masterPath = ".env.master"
$masterValues = Read-EnvFile -path $masterPath

if ($Docker -and $masterValues.Count -eq 0) {
    Write-Host "`n⚠ No .env.master found — generating new one..."

    $pwd = New-RandomString -length 16
    $adminPwd = New-RandomString -length 16

    $masterValues = @{
        DB_PASSWORD            = $pwd
        ADMIN_USERNAME         = 'admin'
        ADMIN_PASSWORD         = $adminPwd
        API_KEY                = New-RandomString -length 32
        AUTH_API_KEY           = New-RandomString -length 32
        AUTH_API_URL           = 'http://auth-api:5555'
        GRAPHQL_API_URL        = 'http://host.docker.internal:5900/graphql'
        STRIPE_API_KEY         = "sk_test_$(New-RandomString -length 32)"
        STRIPE_WEBHOOKS_SECRET = "whsec_$(New-RandomString -length 32)"
        STRIPE_API_URL         = 'http://payment-processor-proxy:4243'
        TWILIO_ACCOUNT_SID     = "AC$(New-RandomString -length 32)"
        TWILIO_AUTH_TOKEN      = New-RandomString -length 32
        TWILIO_MESSAGE_SERVICE_SID = "MG$(New-RandomString -length 32)"
        CACHE_SERVER_ADDRESS   = 'http://warp-cache:7777'
        LOCALIZATION_API_URL   = 'http://localizer-api:8888'
        KAFKA_BROKER_LIST      = 'kafka:29092'
        OIDC_ADMIN_USERNAME    = 'admin'
        OIDC_ADMIN_PASSWORD    = $adminPwd
        Oidc__Realm            = 'nudges'
        Oidc__ServerUrl        = 'https://keycloak:8443'
        OIDC_SERVER_AUTH_URL   = 'https://keycloak.local:8443'
        Oidc__AdminCredentials__AdminClientId = 'admin-cli'
        Oidc__AdminCredentials__Username      = 'admin'
        Oidc__AdminCredentials__Password      = $adminPwd
        IGNORE_SSL_CERT_VALIDATION = 'true'
        Authentication__Schemes__Bearer__RequireHttpsMetadata = 'true'
        Authentication__Schemes__Bearer__Authority            = 'https://keycloak.local:8443/realms/nudges'
        Authentication__Schemes__Bearer__TokenValidationParameters__ValidIssuer = 'https://keycloak.local:8443/realms/nudges'
        Authentication__Schemes__Bearer__TokenValidationParameters__ValidateAudience = 'false'
        IdentityModel__Logging = 'true'
    }

    Write-EnvFile -path $masterPath -values $masterValues -headerComment "Nudges Master Environment Configuration"
} elseif ($Docker) {
    Write-Host "✔ Using existing .env.master"
}

###############################################################################
# Placeholder resolution for Docker mode
###############################################################################

function Get-TemplateDefaults {
    param([string]$searchPath = ".")

    $defaults = @{}
    foreach ($template in Get-ChildItem -Path $searchPath -Filter ".env.template" -Recurse) {
        foreach ($line in Get-Content $template.FullName) {
            $trim = $line.Trim()
            if ($trim -and !$trim.StartsWith('#') -and $trim -match '^([^=]+)=(.*)$') {
                $key = $matches[1].Trim()
                $value = $matches[2].Trim()
                if ($value -notmatch '\{\{.*\}\}' -and -not $defaults.ContainsKey($key)) {
                    if ($value.Length -ge 2 -and (
                            ($value.StartsWith('"') -and $value.EndsWith('"')) -or
                            ($value.StartsWith("'") -and $value.EndsWith("'"))
                        )) {
                        $value = $value.Substring(1, $value.Length - 2)
                    }
                    $defaults[$key] = $value
                }
            }
        }
    }
    return $defaults
}

$templateDefaults = Get-TemplateDefaults

$derived = @{
    KC_DB_USERNAME               = 'keycloak'
    KC_DB_PASSWORD               = $masterValues['DB_PASSWORD']
    KC_BOOTSTRAP_ADMIN_USERNAME  = $masterValues['ADMIN_USERNAME']
    KC_BOOTSTRAP_ADMIN_PASSWORD  = $masterValues['ADMIN_PASSWORD']
    USERDB_PASSWORD              = $masterValues['DB_PASSWORD']
    PRODUCTDB_PASSWORD           = $masterValues['DB_PASSWORD']
    PAYMENTDB_PASSWORD           = $masterValues['DB_PASSWORD']
}

$placeholders = @{}
foreach ($map in @($masterValues, $derived, $templateDefaults)) {
    foreach ($key in $map.Keys) {
        if (-not $placeholders.ContainsKey($key)) {
            $placeholders[$key] = $map[$key]
        }
    }
}

###############################################################################
# Generate .env.docker files if requested
###############################################################################

if ($Docker) {
    Write-Host "`n🚢 Regenerating .env.docker files..."
    Get-ChildItem -Path . -Filter ".env.template" -Recurse | ForEach-Object {
        $outputPath = $_.FullName -replace '\.template$', '.docker'
        Render-Template -templatePath $_.FullName -outputPath $outputPath -values $placeholders
    }
}

###############################################################################
# OIDC client secret extraction
###############################################################################

if ($Oidc) {

    Write-Host "`n🔐 Running OIDC client secret extraction..."

    #
    # STEP 1: Load environment from the keycloak docker env
    # We look for the keycloak .env.docker in the same directory structure
    #

    $keycloakEnvFile = Get-ChildItem -Recurse -Filter "./keycloak/.env.docker" |
                       Select-Object -First 1

    if (-not $keycloakEnvFile) {
        Write-Host "⚠ Could not locate ./keycloak/.env.docker — skipping OIDC extraction."
        Write-Host "   (Keycloak must be generated first via --docker)"
        $Oidc = $false
    }
    else {
        Write-Host "   • Using Keycloak env file: $($keycloakEnvFile.FullName)"

        $envVars = Read-EnvFile -path $keycloakEnvFile.FullName

        $keycloakUrl     = $envVars["KC_URL"]
        $realm           = $envVars["KC_REALM"]
        $adminUser       = $envVars["KEYCLOAK_ADMIN"]
        $adminPassword   = $envVars["KEYCLOAK_ADMIN_PASSWORD"]

        if (-not $keycloakUrl -or -not $realm -or -not $adminUser -or -not $adminPassword) {
            Write-Host "⚠ Missing required Keycloak variables in ./keycloak/.env.docker"
            Write-Host "   Skipping OIDC extraction."
            $Oidc = $false
        }
    }

    #
    # STEP 2: Get admin token
    #

    if ($Oidc) {
        Write-Host "   • Requesting admin token from Keycloak..."

        $PSDefaultParameterValues['Invoke-RestMethod:SkipCertificateCheck'] = $true
        $PSDefaultParameterValues['Invoke-WebRequest:SkipCertificateCheck'] = $true
        try {
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
            Write-Host "   ✔ Successfully authenticated with Keycloak"
        }
        catch {
            Write-Host "❌ Failed to authenticate with Keycloak — cannot extract secrets."

            # Primary error message
            Write-Host "   • Reason: $($_.Exception.Message)"

            # If there's a deeper web exception response
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                Write-Host "   • HTTP Status: $($_.Exception.Response.StatusCode.value__) $($_.Exception.Response.StatusDescription)"
            }

            # Optional verbose detail if you want it
            if ($VerbosePreference -eq 'Continue') {
                Write-Host ""
                Write-Host "—— FULL ERROR (Verbose Mode) ——"
                $_ | Format-List -Force
                Write-Host "———————————————"
            }

            $Oidc = $false
        }
    }

    #
    # STEP 3: Load realm definition to discover client list
    #

    if ($Oidc) {
        $realmFile = Join-Path (Split-Path $keycloakEnvFile.FullName -Parent) "data/realm.json"

        if (-not (Test-Path $realmFile)) {
            Write-Host "❌ Could not locate realm definition at $realmFile"
            Write-Host "   Required to discover clients — aborting OIDC extraction."
            $Oidc = $false
        } else {
            $realmConfig = Get-Content -Path $realmFile -Raw | ConvertFrom-Json

            $services = $realmConfig.clients |
                Where-Object {
                    $_.clientAuthenticatorType -eq "client-secret" -and
                    -not $_.name.Contains("$")
                } |
                Select-Object -ExpandProperty clientId

            Write-Host "   • Found $($services.Count) OIDC client(s) requiring secrets"
        }
    }

    #
    # STEP 4: Iterate clients and generate .env.<service> files
    #

    if ($Oidc) {

        foreach ($service in $services) {

            $secretFilePath = "./keycloak/.env.$service"

            #
            # Option C behavior:
            #   --oidc     → skip if exists
            #   --refresh  → overwrite
            #

            if (-not $Refresh -and (Test-Path $secretFilePath)) {
                Write-Host "   • Secret for $service already exists — skipping (normal --oidc mode)"
                continue
            }

            Write-Host "   • Generating secret for $service"

            # lookup client UUID by clientId
            $clientResponse = Invoke-RestMethod -Method Get `
                -Uri "$keycloakUrl/admin/realms/$realm/clients?clientId=$service" `
                -Headers @{ Authorization = "Bearer $token" }

            $clientId = $clientResponse[0].id

            # request client secret
            $secretResponse = Invoke-RestMethod -Method Post `
                -Uri "$keycloakUrl/admin/realms/$realm/clients/$clientId/client-secret" `
                -Headers @{ Authorization = "Bearer $token" }

            #
            # Write .env.<service>
            #

            $clientIdEntry = "Oidc__ClientId=$service"
            $secretEntry   = "Oidc__ClientSecret=$($secretResponse.value)"

            $content = "$clientIdEntry`n$secretEntry`n"
            Set-Content -Path $secretFilePath -Value $content

            Write-Host "     ✔ Wrote $secretFilePath"
        }

        Write-Host "`n✅ OIDC extraction complete — .env.<service> files ready"
    }
}

###############################################################################
# Local env generation + .NET user-secrets
###############################################################################
if ($Local) {

    Write-Host "`n🏠 Generating local env + .NET user-secrets..."

    $manifestPath = "config/projects.json"

    if (-not (Test-Path $manifestPath)) {
        Write-Host "⚠ Local mode requested, but $manifestPath not found."
        Write-Host "   Skipping local environment configuration."
    }
    else {
        $manifestJson = Get-Content $manifestPath -Raw
        $projectsObj = $manifestJson | ConvertFrom-Json
        
        #
        # Shared local placeholder base
        #
        $baseLocal = @{}
        foreach ($k in $placeholders.Keys) { $baseLocal[$k] = $placeholders[$k] }

        #
        # Iterate manifest entries (KafkaConsumer, AuthApi, etc.)
        #
        foreach ($projProp in $projectsObj.PSObject.Properties) {

            $projName = $projProp.Name
            $projConfig = $projProp.Value

            Write-Host "`n▶ Project: $projName"

            #
            # Validate project path
            #
            $projPath = $projConfig.project
            if (-not (Test-Path $projPath)) {
                Write-Host "   ⚠ Project file not found: $projPath — skipping."
                continue
            }

            #
            # Determine project directory
            #
            $projDir = Split-Path -Parent $projPath

            #
            # Start with base env values
            #
            $localEnv = @{}
            foreach ($k in $baseLocal.Keys) { $localEnv[$k] = $baseLocal[$k] }

            #
            # Merge .env.<service> values (OIDC secrets)
            #
            $envSourcePath = $projConfig.envSource
            if ($envSourcePath -and (Test-Path $envSourcePath)) {
                Write-Host "   • Merging OIDC values from $envSourcePath"
                $envSourceVars = Read-EnvFile -path $envSourcePath
                foreach ($k in $envSourceVars.Keys) {
                    $localEnv[$k] = $envSourceVars[$k]
                }
            }

            #
            # Apply local overrides (localhost topology)
            #
            $localOverrides = $projConfig.localOverrides
            if ($localOverrides) {
                Write-Host "   • Applying local overrides"
                foreach ($p in $localOverrides.PSObject.Properties) {
                    $localEnv[$p.Name] = $p.Value
                }
            }

            #
            # Load per-project .env.template to determine required keys
            #
            $projTemplatePath = Join-Path $projDir ".env.template"
            $requiredKeys = @()

            if (Test-Path $projTemplatePath) {
                Write-Host "   • Using .env.template to determine required keys"
                $templateLines = Get-Content $projTemplatePath
                foreach ($line in $templateLines) {
                    if ($line -match '^([^=]+)=') {
                        $requiredKeys += $matches[1].Trim()
                    }
                }
            } else {
                Write-Host "   • No .env.template found — using full environment set"
                $requiredKeys = $localEnv.Keys
            }

            #
            # Filter localEnv down to only required keys
            #
            $filteredValues = @{}
            foreach ($key in $requiredKeys) {
                if ($localEnv.ContainsKey($key)) {
                    $filteredValues[$key] = $localEnv[$key]
                }
            }

            #
            # Drift warnings
            #
            $undefinedKeys = $requiredKeys | Where-Object { -not $localEnv.ContainsKey($_) }
            if ($undefinedKeys.Count -gt 0) {
                Write-Host "   ⚠ Template references keys that are not defined:"
                foreach ($k in $undefinedKeys) { Write-Host "      - $k" }
            }

            # $unusedKeys = $localEnv.Keys | Where-Object { $_ -notin $requiredKeys }
            # if ($unusedKeys.Count -gt 0) {
            #     Write-Host "   ⚠ Environment contains additional keys not in template:"
            #     foreach ($k in $unusedKeys) { Write-Host "      - $k" }
            # }

            #
            # Build user-secret map (from filtered values)
            #
            $secretValues = @{}
            foreach ($entry in $filteredValues.GetEnumerator()) {
                $secretValues[$entry.Key] = $entry.Value
            }

            #
            # Apply OIDC remapping (env notation → colon notation)
            #
            $oidcConfig = $projConfig.oidc
            if ($oidcConfig -and $oidcConfig.map) {
                Write-Host "   • Mapping OIDC keys for user-secrets"
                foreach ($kv in $oidcConfig.map.PSObject.Properties) {
                    $envKey = $kv.Name
                    $configKey = $kv.Value
                    if ($secretValues.ContainsKey($envKey)) {
                        $secretValues[$configKey] = $secretValues[$envKey]
                        $null = $secretValues.Remove($envKey)
                    }
                }
            }

            #
            # Generate project-adjacent .env.local (just for reference, really)
            #
            $projEnvLocalPath = Join-Path $projDir ".env.local"
            Write-EnvFile -path $projEnvLocalPath -values $filteredValues -headerComment "Local environment for $projName"

            #
            # Populate .NET user-secrets
            #
            Write-Host "   • Updating .NET user-secrets"

            # prevent first-run hangs
            $env:DOTNET_NOLOGO = "1"
            $env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
            $env:DOTNET_GENERATE_ASPNET_CERTIFICATE = "false"

            $dotnetArgsBase = @("user-secrets", "--project", $projPath)

            # Ensure project has a secrets ID
            $null = & dotnet @($dotnetArgsBase + "list") 2>$null
            if ($LASTEXITCODE -ne 0) {
                Write-Host "   • Initializing UserSecretsId"
                $null = & dotnet @($dotnetArgsBase + "init")
            }

            # Clear existing secrets
            $null = & dotnet @($dotnetArgsBase + "clear")

            # Apply filtered + mapped secrets
            foreach ($entry in $secretValues.GetEnumerator()) {
                $key = $entry.Key
                $value = [string]$entry.Value
                $null = & dotnet @($dotnetArgsBase + @("set", $key, $value))
            }

            Write-Host "   ✔ Local env + user-secrets updated for $projName"
        }
    }
}


###############################################################################
# Final summary
###############################################################################

Write-Host "`n✅ Configuration generation complete."

if ($Docker) { Write-Host "   • .env.docker regenerated" }
if ($Oidc)   { Write-Host "   • .env.<service> OIDC secrets generated" }
if ($Local)  { Write-Host "   • .env.<service>.local + .NET secrets updated" }
if ($Refresh){ Write-Host "   • Full regeneration performed" }
