$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$envFile = Join-Path $scriptDir ".env"

if (-not (Test-Path $envFile)) {
    Write-Host "The .env file at '$envFile' doesn't exist. Run 'npx dotenv-vault pull'."
    exit 1
}

# Read the .env file line by line
$envLines = Get-Content $envFile
foreach ($line in $envLines) {
    # Ignore lines starting with '#' (comments) and empty lines
    if ($line -match "^\s*#") {
        continue
    }
    if (-not $line.Trim()) {
        continue
    }

    # Split the line into key and value
    $key, $value = $line -split '=', 2

    $key = $key.Trim()
    $value = $value.Trim() -replace '"', ''
  
    # Set the environment variable
    [Environment]::SetEnvironmentVariable($key, $value.Trim())
}

# Print the environment variables (optional)
Write-Host "Environment variables set:"
Get-ChildItem -Path "Env:" | Sort-Object Name | ForEach-Object {
    Write-Host "$($_.Name) = $($_.Value)"
}



