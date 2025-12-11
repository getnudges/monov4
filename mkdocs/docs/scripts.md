# Scripts

This page documents the PowerShell scripts used for development and maintenance.

## Root Scripts

### start-dev.ps1

Main development startup script. Starts all Docker services in the correct order.

```powershell
./start-dev.ps1
```

**What it does:**

1. Loads environment from `.env.external`
2. Starts infrastructure services (Kafka, PostgreSQL, Redis, etc.)
3. Waits for Kafka broker to be ready
4. Starts application services (APIs, Gateway, Web)
5. Opens browser to `https://localhost:5050`

### configure.ps1

Generates `.env` files for services based on deployment mode.

```powershell
./configure.ps1 -Mode <Docker|Oidc|Local>
```

**Modes:**

| Mode | Description |
|------|-------------|
| `Docker` | All services run in Docker containers (default) |
| `Oidc` | Uses external OIDC provider |
| `Local` | Services run on host machine |

**Generated files:**

- `dotnet/*/.env` - Service-specific environment variables
- Merges values from `.env.external` (API keys, secrets)

### reset-env.ps1

Removes all generated `.env` files from the dotnet directory.

```powershell
./reset-env.ps1
```

Use this to clean up before switching configuration modes.

### clean-dotnet.ps1

Removes all `bin/` and `obj/` directories from dotnet projects.

```powershell
./clean-dotnet.ps1
```

Useful for forcing a clean rebuild or resolving build issues.

## Certificate Scripts

### certs/generate-certs.ps1

Generates SSL certificates for local HTTPS development.

```powershell
# First, create the PFX file
dotnet dev-certs https -ep ./certs/aspnetapp.pfx

# Then generate PEM files from it
./certs/generate-certs.ps1
```

**Generates:**

- `aspnetapp.crt` - Certificate file
- `aspnetapp.key` - Private key file

These are used by nginx and other services that require PEM-format certificates.

## Database Scripts

Located in the `db/` directory.

### run-migrations.ps1

Runs Entity Framework migrations for a specific database context.

```powershell
cd db
./run-migrations.ps1 -Context <ContextName> -Project <ProjectPath>
```

**Parameters:**

| Parameter | Description |
|-----------|-------------|
| `-Context` | EF DbContext name (e.g., `UserDbContext`) |
| `-Project` | Path to the project containing the context |

**Example:**

```powershell
./run-migrations.ps1 -Context UserDbContext -Project ../dotnet/UserApi
```

### create-userdb-migration.ps1

Creates a new migration for UserDb.

```powershell
cd db
./create-userdb-migration.ps1 <MigrationName>
```

**Example:**

```powershell
./create-userdb-migration.ps1 AddPhoneVerificationTable
```

Similar scripts exist for other databases:

- `create-productdb-migration.ps1`
- `create-paymentdb-migration.ps1`

### scaffold-*.ps1

Scaffolds entity classes from existing database tables (database-first approach).

```powershell
./scaffold-userdb.ps1
```

## Gateway Build Script

### build-gateway.ps1

Located at `dotnet/GraphQLGateway/GraphQLGateway/build-gateway.ps1`.

Builds the HotChocolate Fusion gateway configuration by composing subgraph schemas.

```powershell
cd dotnet/GraphQLGateway/GraphQLGateway
./build-gateway.ps1
```

**What it does:**

1. Packs each subgraph schema (UserApi, ProductApi, PaymentApi)
2. Composes them into a unified gateway configuration
3. Outputs `gateway.fgp` (Fusion Gateway Package)

**When to run:**

- After modifying any GraphQL schema in the subgraph APIs
- After adding new queries, mutations, or subscriptions
- When setting up a fresh development environment

The gateway must be rebuilt whenever subgraph schemas change, or the gateway won't expose the new operations.
