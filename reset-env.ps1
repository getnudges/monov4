#!/usr/bin/env pwsh
<#
    Deletes all generated env files so configuration can be rebuilt cleanly.

    Removes:
      • *.env.local
      • *.env.docker

    Preserves:
      • .env.master
      • *.env.template
      • .env.* (service OIDC secret files)
#>

Write-Host "🔍 Scanning for generated environment files..." -ForegroundColor Cyan

$files = Get-ChildItem -Recurse -File |
    Where-Object {
        $_.Name -like "*.env.local" -or
        $_.Name -like "*.env.docker"
    }

if ($files.Count -eq 0) {
    Write-Host "✅ No generated env files found."
    exit 0
}

Write-Host "`n🗑 Removing $($files.Count) files:`n" -ForegroundColor Yellow

foreach ($file in $files) {
    Write-Host "   • $($file.FullName)"
    Remove-Item $file.FullName -Force
}

Write-Host "`n✅ Environment reset complete — all .env.local and .env.docker files removed." -ForegroundColor Green
