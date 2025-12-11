# Database Migrator

The database migrator uses **Entity Framework Core** to manage schema changes across three PostgreSQL databases. Each database has its own DbContext and independent migration history.

## Databases

| Database | DbContext | Purpose |
|----------|-----------|---------|
| `userdb` | `UserDbContext` | Users, clients, subscribers, admins |
| `productdb` | `ProductDbContext` | Plans, pricing, subscriptions, trials, discounts |
| `paymentdb` | `PaymentDbContext` | Payment confirmations, merchant services |

## Running Migrations

### Locally

From the `db/` directory:

```powershell
.\run-migrations.ps1 UserDbContext
.\run-migrations.ps1 ProductDbContext
.\run-migrations.ps1 PaymentDbContext
```

### In Docker/Deployment

The `Migrator.Dockerfile` creates self-contained executables for each database:

```bash
./migrateUserDb
./migrateProductDb
./migratePaymentDb
```

These bundles don't require .NET runtime on the target system.

## Creating Migrations

### Workflow

1. Modify your model classes in `Nudges.Data/<Domain>/`
2. Create the migration
3. Apply and test locally
4. Commit migration files

### Commands

From the `db/` directory:

```powershell
# UserDb
.\create-userdb-migration.ps1 "AddNewColumn"

# ProductDb
.\create-productdb-migration.ps1 "AddDiscountTable"

# PaymentDb
.\create-paymentdb-migration.ps1 "AddPaymentStatus"
```

Migration files are created in:

- `dotnet/Nudges.Data/Users/Migrations/`
- `dotnet/Nudges.Data/Products/Migrations/`
- `dotnet/Nudges.Data/Payments/Migrations/`

## Helper Scripts

| Script | Purpose |
|--------|---------|
| `run-migrations.ps1 <Context>` | Apply pending migrations |
| `create-userdb-migration.ps1 "Name"` | Create UserDb migration |
| `create-productdb-migration.ps1 "Name"` | Create ProductDb migration |
| `create-paymentdb-migration.ps1 "Name"` | Create PaymentDb migration |
| `scaffold-userdb.ps1` | Reverse-engineer models from existing DB |
| `scaffold-productdb.ps1` | Reverse-engineer models from existing DB |
| `scaffold-paymentdb.ps1` | Reverse-engineer models from existing DB |

## Configuration

Required environment variables:

```ini
ConnectionStrings__UserDb=Host=localhost;Database=userdb;Username=userdb;Password=...
ConnectionStrings__ProductDb=Host=localhost;Database=productdb;Username=productdb;Password=...
ConnectionStrings__PaymentDb=Host=localhost;Database=paymentdb;Username=paymentdb;Password=...
```

## Notes

- Each DbContext maintains **independent migration history** - create and apply migrations separately
- PostgreSQL columns use `snake_case`, C# properties use `PascalCase`
- Don't edit `*ModelSnapshot.cs` files manually
- Migration names should use PascalCase (e.g., `AddPhoneEncryption`)
