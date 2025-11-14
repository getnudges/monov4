# Extract certificate using .NET
$pfxPath = "${PWD}\aspnetapp.pfx"
$password = Read-Host "Enter PFX password" -AsSecureString
$pfx = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($pfxPath, $password, [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable)

# Export certificate (public key) as Base64 encoded CRT
$certBytes = $pfx.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
$certBase64 = [System.Convert]::ToBase64String($certBytes)
$certPem = "-----BEGIN CERTIFICATE-----`n" + ($certBase64 -replace '(.{64})', "`$1`n") + "`n-----END CERTIFICATE-----"
$certPem | Out-File -FilePath "aspnetapp.crt" -Encoding ASCII

# Export private key (requires additional steps)
$privateKey = [System.Security.Cryptography.X509Certificates.RSACertificateExtensions]::GetRSAPrivateKey($pfx)
$privateKeyBytes = $privateKey.ExportRSAPrivateKey()
$keyBase64 = [System.Convert]::ToBase64String($privateKeyBytes)
$keyPem = "-----BEGIN RSA PRIVATE KEY-----`n" + ($keyBase64 -replace '(.{64})', "`$1`n") + "`n-----END RSA PRIVATE KEY-----"
$keyPem | Out-File -FilePath "aspnetapp.key" -Encoding ASCII
