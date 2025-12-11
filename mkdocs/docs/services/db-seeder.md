# Database Seeder

The DbSeeder is a CLI tool that populates initial data into PostgreSQL databases and Redis cache.

## Commands

```bash
# Seed PostgreSQL databases
dotnet run -- db

# Seed Redis cache with Stripe products
dotnet run -- redis
```

## What It Seeds

### `db` Command

Seeds the PostgreSQL user database with:

- **Default admin client** - Phone: `+15555555555`, slug: `nudges`, name: `Nudges`

### `redis` Command

Fetches data from Stripe and caches in Redis:

- **Product prices** - Active service-type products from Stripe
- **Price metadata** - Limits and other price properties

## Running

### Local Development

```powershell
cd dotnet
dotnet run --project tools/DbSeeder/DbSeeder.csproj -- db
dotnet run --project tools/DbSeeder/DbSeeder.csproj -- redis
```

### Docker

```bash
# Build
docker build -f dotnet/DbSeeder.Dockerfile -t dbseeder:latest .

# Run db seeding (default)
docker run --env-file .env dbseeder:latest

# Run redis seeding
docker run --env-file .env dbseeder:latest redis
```

## Configuration

Required environment variables:

```ini
# Database connections
ConnectionStrings__UserDb=Host=localhost;Database=userdb;...
ConnectionStrings__ProductDb=Host=localhost;Database=productdb;...
ConnectionStrings__PaymentDb=Host=localhost;Database=paymentdb;...

# Security
HashSettings__HashKeyBase64=<base64-key>
EncryptionSettings__Key=<base64-key>

# Redis seeding
REDIS_URL=localhost:6379
STRIPE_API_KEY=sk_...
```

## Notes

- The `db` command expects a default admin user to already exist
- Phone numbers are hashed before storage
- Sensitive fields use AES-GCM encryption via EF Core interceptors
