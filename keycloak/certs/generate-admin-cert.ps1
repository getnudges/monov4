$certParams = @{
    DnsName           = "localhost"
    CertStoreLocation = "Cert:\CurrentUser\My"
    KeyExportPolicy   = "Exportable"
    KeySpec           = "KeyExchange"
    NotAfter          = (Get-Date).AddYears(1)
}

$cert = New-SelfSignedCertificate @certParams

$certPath = "Cert:\CurrentUser\My\$($cert.Thumbprint)"
$pwd = ConvertTo-SecureString -String "temp" -Force -AsPlainText

# Export to PFX
Export-PfxCertificate -Cert $certPath -FilePath ".\admin.pfx" -Password $pwd

# Convert to PEM using certutil (Windows)
certutil -encode ".\admin.pfx" ".\admin.pfx.b64"
Get-Content ".\admin.pfx.b64" | Select-Object -Skip 1 | Select-Object -SkipLast 1 | Out-File ".\admin.crt" -Encoding ascii

# Extract private key (requires OpenSSL or we can use existing tls.key as template)
Write-Host "Certificate generated at admin.pfx"
Write-Host "For production, convert PFX to PEM format with proper tools"
Write-Host ""
Write-Host "Quick workaround: Copy existing tls.crt/tls.key:"
Copy-Item ".\tls.crt" ".\admin.crt" -Force
Copy-Item ".\tls.key" ".\admin.key" -Force
Write-Host "admin.crt and admin.key created (using existing keycloak certs)"

# Cleanup
Remove-Item $certPath
Remove-Item ".\admin.pfx" -ErrorAction SilentlyContinue
Remove-Item ".\admin.pfx.b64" -ErrorAction SilentlyContinue
