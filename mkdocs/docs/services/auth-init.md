# AuthInit

AuthInit is a one-time initialization CLI tool that sets up the default admin user in both the PostgreSQL database and Keycloak. Run this once during initial system setup before starting other services.

## What It Does

1. Creates default admin user in the database (phone: `+15555555555`)
2. Creates matching admin user in Keycloak with group `admins`
3. Links the database record to the Keycloak user ID (Subject)

## Running

### Command Line

```bash
dotnet Nudges.AuthInit.dll seed
```

### Docker

```bash
docker compose up auth-init
```

### Local Development

```powershell
cd dotnet/Nudges.AuthInit
dotnet run -- seed
```

## Configuration

Required environment variables:

```ini
# Database
ConnectionStrings__UserDb=Host=localhost;Database=userdb;...

# Security
HashSettings__HashKeyBase64=<base64-key>
EncryptionSettings__Key=<base64-key>

# Keycloak
Oidc__Realm=nudges
Oidc__ServerUrl=https://keycloak.local:8443
Oidc__ClientId=auth-init
Oidc__ClientSecret=<secret>
Oidc__AdminCredentials__Username=admin
Oidc__AdminCredentials__Password=<password>
```

## Notes

- **Idempotent** - Safe to run multiple times; skips if admin already exists
- **Run first** - Other services depend on the admin user existing
- **Development password** - Default Keycloak password is `pass`
- Phone numbers are hashed in the database but stored as plaintext username in Keycloak
