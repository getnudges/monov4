#!/usr/bin/env pwsh
<#
    Deletes all generated env files so configuration can be rebuilt cleanly.

    Removes:
      • *.env.local
      • *.env.docker
            • keycloak/.env.* (except .env.template)

    Preserves:
            • .env.master (unless -Force)
            • *.env.template
#>

param(
    [switch]$Force
)

Write-Host "🔍 Scanning for generated environment files..." -ForegroundColor Cyan

$files = Get-ChildItem -Recurse -File |
Where-Object {
    $_.Name -like "*.env.local" -or
    $_.Name -like "*.env.docker" -or
    ($Force -and $_.Name -eq ".env.master") -or
    (($_.FullName -match "[\\/]keycloak[\\/]") -and ($_.Name -like ".env.*") -and ($_.Name -ne ".env.template"))
}

if ($files.Count -eq 0) {
    if ($Force) {
        Write-Host "✅ No generated env files found (including .env.master)."
    }
    else {
        Write-Host "✅ No generated env files found."
    }
    exit 0
}

Write-Host "`n🗑 Removing $($files.Count) files:`n" -ForegroundColor Yellow

foreach ($file in $files) {
    Write-Host "   • $($file.FullName)"
    Remove-Item $file.FullName -Force
}

if ($Force) {
    Write-Host "`n✅ Environment reset complete — removed .env.local, .env.docker, keycloak/.env.* (except template), and .env.master." -ForegroundColor Green
}
else {
    Write-Host "`n✅ Environment reset complete — removed .env.local, .env.docker, and keycloak/.env.* (except template)." -ForegroundColor Green
}
