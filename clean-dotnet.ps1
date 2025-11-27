#!/usr/bin/env pwsh
<#
    Deletes all build output directories (bin/ and obj/) under the dotnet/ folder.

    Usage:
      ./clean-dotnet.ps1                 # cleans d:\monov5\dotnet by default
      ./clean-dotnet.ps1 -DryRun         # shows what would be removed
      ./clean-dotnet.ps1 -Root ./dotnet  # specify a different root
#>

[CmdletBinding()]
param(
    # Root directory to scan (defaults to the repo's dotnet folder)
    [string]$Root = (Join-Path $PSScriptRoot 'dotnet'),
    # Show what would be removed without deleting
    [switch]$DryRun
)

if (-not (Test-Path -LiteralPath $Root)) {
    Write-Host "⚠ Root not found: $Root" -ForegroundColor Yellow
    exit 1
}

Write-Host "🧹 Cleaning build outputs under: $Root" -ForegroundColor Cyan

# Find all bin/ and obj/ directories (including hidden), deepest paths first
$targets = Get-ChildItem -LiteralPath $Root -Recurse -Directory -Force -ErrorAction SilentlyContinue |
Where-Object { $_.Name -in @('bin', 'obj') } |
Sort-Object { $_.FullName.Length } -Descending

if (-not $targets -or $targets.Count -eq 0) {
    Write-Host "✅ No bin/ or obj/ folders found." -ForegroundColor Green
    exit 0
}

Write-Host "Found $($targets.Count) directories to remove:" -ForegroundColor Yellow
$targets | ForEach-Object { Write-Host "   • $($_.FullName)" }

if ($DryRun) {
    Write-Host "\nℹ Dry run only — nothing was deleted." -ForegroundColor DarkYellow
    exit 0
}

$errors = @()
foreach ($dir in $targets) {
    try {
        # Remove directory recursively; -Force to handle read-only items
        Remove-Item -LiteralPath $dir.FullName -Recurse -Force -ErrorAction Stop
    }
    catch {
        $errors += $_
        Write-Host "   ✖ Failed: $($dir.FullName) — $($_.Exception.Message)" -ForegroundColor Red
    }
}

if ($errors.Count -eq 0) {
    Write-Host "\n✅ Clean complete — removed $($targets.Count) directories." -ForegroundColor Green
}
else {
    Write-Host "\n⚠ Clean finished with $($errors.Count) error(s)." -ForegroundColor Yellow
}
