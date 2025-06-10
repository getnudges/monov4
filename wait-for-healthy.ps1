param(
    [Parameter(Mandatory=$true)]
    [string]$ContainerName
)

Write-Host "Checking health for container '$ContainerName'..."

while ((docker inspect --format='{{.State.Status}}' $ContainerName) -ne "healthy") {
    Write-Host "Waiting for $ContainerName to become healthy..."
    Start-Sleep -Seconds 2
}

Write-Host "$ContainerName is healthy!"
