# Copilot Instructions for Nudges System (monov4)

## Big Picture Architecture

- **Monorepo Structure:** Contains multiple services and components for the Nudges system, including backend APIs (.NET), databases, web frontends, infrastructure, and documentation.
- **Service Boundaries:**
  - `.NET` services in `dotnet/` (e.g., `AuthApi`, `PaymentApi`, `ProductApi`, etc.)
  - Database setup and migrations in `db/`
  - Frontend apps in `web/`
  - Infrastructure as code in `terraform/`
  - Documentation in `mkdocs/`
- **Data Flow:** Most backend services communicate via REST/gRPC or GraphQL, with databases provisioned via Docker and scripts in `db/`. Localization uses a dedicated gRPC service (`Nudges.Localization.Client`).

## Developer Workflows

- **Build & Test:**
  - Use PowerShell scripts in the repo root and `dotnet/` for builds, migrations, and tests (e.g., `start-dev.ps1`, `run-tests.ps1`).
  - Database setup: Run scripts in `db/` (see `db/README.md`). Requires `.env.docker` for local DB setup.
  - Frontend: Vite + React + TypeScript. See `web/new-signup/README.md` and `web/new-admin/README.md` for ESLint and build config.
  - Docs: Serve with `mkdocs serve` after Python venv setup (`mkdocs/README.md`).
- **Debugging:**
  - Most .NET services support local debugging via standard .NET CLI and Dockerfiles in `dotnet/`.
  - Database containers can be rebuilt and initialized using scripts in `db/`.

## Project-Specific Conventions

- **PowerShell-first scripting:** Most automation uses `.ps1` scripts for Windows compatibility.
- **Database migrations:** Add SQL scripts to `db/`, update `Dockerfile` and `init-db.sh` to include new scripts.
- **Environment files:** `.env.docker` required for local DB setup (see template in `db/README.md`).
- **Frontend linting:** Use type-aware ESLint configs as described in `web/new-signup/README.md`.
- **Localization:** Use `Nudges.Localization.Client` for gRPC-based localization in .NET (see example in `dotnet/Nudges.Localization.Client/README.md`).

## Integration Points & External Dependencies

- **Docker:** Used for DB, some backend services, and Stripe API emulation.
- **Terraform:** Manages cloud infrastructure, but some secrets (e.g., SSH keys) must be created manually (see `terraform/README.md`).
- **Keycloak:** Used for authentication, configured in `keycloak/`.
- **Kafka:** Messaging setup in `db/kafka.json` and related scripts.
- **Stripe:** API emulation in `stripe/`.

## Cross-Component Communication

- **gRPC:** Used for localization and possibly other internal APIs.
- **GraphQL:** Standard for most backend APIs.
- **REST:** Used for some communication (especially around authentication).
- **Kafka:** For event-driven communication between services.

## Examples & References

- **Database migration pattern:** Add SQL to `db/`, update `Dockerfile` and `init-db.sh`.
- **Frontend lint config:** See `web/new-signup/README.md` for ESLint setup.
- **Localization usage:** See `dotnet/Nudges.Localization.Client/README.md` for DI and usage examples.
- **Infrastructure manual steps:** See `terraform/README.md` for secrets and DNS setup.

---

**If any section is unclear or missing key patterns, please provide feedback or point to specific files for deeper analysis.**
