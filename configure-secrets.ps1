#!/usr/bin/env pwsh

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

# Check if .env.master exists
$masterEnvPath = ".env.master"
if (-not (Test-Path $masterEnvPath)) {
    Write-Error ".env.master not found. Please run ./initialize-env.ps1 first."
    exit 1
}

Write-Host "Reading .env.master..."
$masterValues = Read-EnvMaster -path $masterEnvPath

if ($masterValues.Count -eq 0) {
    Write-Error ".env.master is empty or invalid."
    exit 1
}

# List of projects to configure
$projects = @(
    # "dotnet/AuthApi/AuthApi.csproj"
    # "dotnet/UserApi/UserApi.csproj"
    # "dotnet/PaymentApi/PaymentApi.csproj"
    "dotnet/ProductApi/ProductApi.csproj"
    # "dotnet/KafkaConsumer/KafkaConsumer.csproj"
)

foreach ($projectPath in $projects) {
    $fullPath = Join-Path -Path "." -ChildPath $projectPath
    
    if (-not (Test-Path $fullPath)) {
        Write-Warning "Project not found: $fullPath - skipping"
        continue
    }
    
    Write-Host "`nConfiguring user secrets for: $projectPath"
    
    # Set user secrets for this project
    Push-Location (Split-Path $fullPath -Parent)
    
    try {
        # Set each value from .env.master as a user secret
        foreach ($key in $masterValues.Keys) {
            Write-Host "  Setting $key..."
            dotnet user-secrets set $key $masterValues[$key] --project (Split-Path $fullPath -Leaf)
        }
        
        Write-Host "Successfully configured secrets for $projectPath" -ForegroundColor Green
    }
    catch {
        Write-Error "Failed to configure secrets for ${projectPath}: $($_.Exception.Message)"
    }
    finally {
        Pop-Location
    }
}

Write-Host "`nUser secrets configuration complete!"
