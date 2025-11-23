$ErrorActionPreference = "Stop"

# Extract certificate using .NET
$pfxPath = "$PSScriptRoot\aspnetapp.pfx"
$password = Read-Host "Enter PFX password" -AsSecureString
$passwordPlain = (New-Object System.Net.NetworkCredential("", $password)).Password
dotnet dev-certs https -ep $pfxPath -p $passwordPlain
$pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $passwordPlain, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

# Export certificate (public key) as Base64 encoded CRT
$certBytes = $pfx.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
$certBase64 = [System.Convert]::ToBase64String($certBytes)
$certPem = "-----BEGIN CERTIFICATE-----`n" + ($certBase64 -replace '(.{64})', "`$1`n") + "`n-----END CERTIFICATE-----"
$certPem | Out-File -FilePath "$PSScriptRoot\aspnetapp.crt" -Encoding ASCII

# Export private key (requires additional steps)
$privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($pfx)
$privateKeyBytes = $privateKey.ExportRSAPrivateKey()
$keyBase64 = [System.Convert]::ToBase64String($privateKeyBytes)
$keyPem = "-----BEGIN RSA PRIVATE KEY-----`n" + ($keyBase64 -replace '(.{64})', "`$1`n") + "`n-----END RSA PRIVATE KEY-----"
$keyPem | Out-File -FilePath "$PSScriptRoot\aspnetapp.key" -Encoding ASCII

# Create symbolic links for web/new-signup
$signupDir = Join-Path (Split-Path $PSScriptRoot -Parent) "web\new-signup"
$adminDir = Join-Path (Split-Path $PSScriptRoot -Parent) "web\admin-ui"
$signupCrt = Join-Path $signupDir "aspnetapp.crt"
$signupKey = Join-Path $signupDir "aspnetapp.key"
$adminCrt = Join-Path $adminDir "aspnetapp.crt"
$adminKey = Join-Path $adminDir "aspnetapp.key"

# Remove existing symlinks or files if they exist
if (Test-Path $signupCrt) { Remove-Item $signupCrt -Force }
if (Test-Path $signupKey) { Remove-Item $signupKey -Force }
if (Test-Path $adminCrt) { Remove-Item $adminCrt -Force }
if (Test-Path $adminKey) { Remove-Item $adminKey -Force }

# Create symbolic links (requires admin privileges on Windows)
New-Item -ItemType SymbolicLink -Path $signupCrt -Target "$PSScriptRoot\aspnetapp.crt" -Force | Out-Null
New-Item -ItemType SymbolicLink -Path $signupKey -Target "$PSScriptRoot\aspnetapp.key" -Force | Out-Null
New-Item -ItemType SymbolicLink -Path $adminCrt -Target "$PSScriptRoot\aspnetapp.crt" -Force | Out-Null
New-Item -ItemType SymbolicLink -Path $adminKey -Target "$PSScriptRoot\aspnetapp.key" -Force | Out-Null

Write-Host "✓ Certificates generated and symlinked to web/new-signup"
Write-Host "✓ Certificates generated and symlinked to web/admin-ui"
