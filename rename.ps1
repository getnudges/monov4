param(
    [Parameter(Mandatory = $true)]
    [string]$Path,
    [string[]]$FileExtensions = @("*.txt", "*.cs", "*.js", "*.html", "*.xml", "*.json", "*.config", "*.md", "*.yml", "*.yaml"),
    [switch]$WhatIf
)

function Get-CasePreservingReplacement {
    param(
        [string]$Original
    )
    
    # Define case variations as array of objects
    $replacements = @(
        @{ Pattern = "UnAd"; Replacement = "Nudges" },
        @{ Pattern = "UNAD"; Replacement = "NUDGES" },
        @{ Pattern = "unad"; Replacement = "nudges" },
        @{ Pattern = "Unad"; Replacement = "Nudges" },
        @{ Pattern = "unAd"; Replacement = "nudges" },
        @{ Pattern = "UnAD"; Replacement = "Nudges" }
    )
    
    $result = $Original
    foreach ($item in $replacements) {
        # Use case-sensitive replacement
        $result = $result -creplace [regex]::Escape($item.Pattern), $item.Replacement
    }
    
    return $result
}

function Update-FileContent {
    param(
        [string]$FilePath,
        [switch]$WhatIf
    )
    
    try {
        $content = Get-Content -Path $FilePath -Raw -ErrorAction Stop
        $originalContent = $content
        
        # Replace all case variations
        $content = Get-CasePreservingReplacement -Original $content
        
        if ($content -ne $originalContent) {
            if ($WhatIf) {
                Write-Host "WOULD UPDATE FILE: $FilePath" -ForegroundColor Yellow
            }
            else {
                Set-Content -Path $FilePath -Value $content -NoNewline
                Write-Host "UPDATED FILE: $FilePath" -ForegroundColor Green
            }
            return $true
        }
    }
    catch {
        Write-Warning "Failed to process file $FilePath`: $($_.Exception.Message)"
    }
    
    return $false
}

function Rename-ItemsWithPattern {
    param(
        [string]$BasePath,
        [switch]$WhatIf
    )
    
    # Get all files and directories, process deepest first to avoid path issues
    $items = Get-ChildItem -Path $BasePath -Recurse | Sort-Object FullName -Descending
    
    foreach ($item in $items) {
        $newName = Get-CasePreservingReplacement -Original $item.Name
        
        if ($newName -ne $item.Name) {
            $newPath = Join-Path $item.Parent.FullName $newName
            
            if ($WhatIf) {
                Write-Host "WOULD RENAME: $($item.FullName) -> $newPath" -ForegroundColor Cyan
            }
            else {
                try {
                    Rename-Item -Path $item.FullName -NewName $newName -ErrorAction Stop
                    Write-Host "RENAMED: $($item.FullName) -> $newPath" -ForegroundColor Blue
                }
                catch {
                    Write-Warning "Failed to rename $($item.FullName): $($_.Exception.Message)"
                }
            }
        }
    }
}

# Main script execution
if (-not (Test-Path $Path)) {
    Write-Error "Path '$Path' does not exist."
    exit 1
}

Write-Host "Starting UnAd to Nudges replacement..." -ForegroundColor White
Write-Host "Target Path: $Path" -ForegroundColor White
Write-Host "File Extensions: $($FileExtensions -join ', ')" -ForegroundColor White

if ($WhatIf) {
    Write-Host "RUNNING IN WHATIF MODE - NO CHANGES WILL BE MADE" -ForegroundColor Magenta
}

Write-Host "`n--- Processing File Contents ---" -ForegroundColor White

# Process file contents
$fileCount = 0
$updatedFiles = 0

foreach ($extension in $FileExtensions) {
    $files = Get-ChildItem -Path $Path -Filter $extension -Recurse -File
    
    foreach ($file in $files) {
        $fileCount++
        if (Update-FileContent -FilePath $file.FullName -WhatIf:$WhatIf) {
            $updatedFiles++
        }
    }
}

Write-Host "`n--- Processing File and Directory Names ---" -ForegroundColor White

# Process file and directory names
Rename-ItemsWithPattern -BasePath $Path -WhatIf:$WhatIf

Write-Host "`n--- Summary ---" -ForegroundColor White
Write-Host "Files processed: $fileCount" -ForegroundColor White
Write-Host "Files updated: $updatedFiles" -ForegroundColor White

if ($WhatIf) {
    Write-Host "`nTo execute the changes, run the script without the -WhatIf parameter." -ForegroundColor Yellow
}
else {
    Write-Host "`nReplacement complete!" -ForegroundColor Green
}
