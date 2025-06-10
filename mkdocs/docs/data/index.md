# UnAd Data Layer Overview

UnAd roughly follows a [Domain-driven design](https://en.wikipedia.org/wiki/Domain-driven_design) concept with it's data persistence layer, which has many implications for this system.

## DDD Approach

Following a DDD pattern, our data exists in multiple separate databases, each for a distinct domain.

- The user-related data all lives in the [`userdb` database](/data/userdb), implemented in PostgresQL.
- The product-related data all lives in the [`productdb` database](/data/productdb), implemented in PostgresQL.
- The payment-related data all lives in the [`paymentdb` database](/data/paymentdb), implemented in PostgresQL.
- Ephemeral data and some cached data lives in a Redis database. 

Management of the databases in UnAd are handled with a mix of local scripts to modify the local databases, and [EF Core Migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/?tabs=dotnet-core-cli).

## Conventions

By convention, any changes needed to our databases should be done locally by any means seen fit at the time, and when those changes are finalized, the [migration scripts](#migrations) should be run to ensure this change can be deployed in a consistent and recoverable way.

## Migrations

In the `db` directory, there are scripts for each Postgres database to handle modification tasks, each named for the relevant db.

- `create-paymentdb-migration.ps1` generates a migration using the specified name for the PaymentDbContext and it's relevant database.  To call this script, `cd` into the `db` directory, and run it with the name of the new migration:  `./create-paymentdb-migration.ps1 MyNewMigration`.  ***Remember to use C# class naming conventions for the migration name***.
