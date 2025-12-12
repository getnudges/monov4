# Docker

The system runs entirely in Docker for local development. All services are defined in `docker-compose.yml`.

## Quick Start

```powershell
# Generate certificates
dotnet dev-certs https -ep ./certs/aspnetapp.pfx
./certs/generate-certs.ps1

# Start everything
./start-dev.ps1
```

First run takes ~20 minutes to build. Once ready, open `https://localhost:5050`.

## Services Overview

### Infrastructure

| Service | Port | Description |
|---------|------|-------------|
| `postgres` | 5432 | PostgreSQL database |
| `redis` | 6379 | Redis cache |
| `kafka` | 9092, 29092 | Kafka message broker |
| `kafka-ui` | 8080 | Kafka management UI |
| `keycloak` | 8443 | OIDC identity provider |

### Observability

| Service | Port | Description |
|---------|------|-------------|
| `grafana` | 3000 | Dashboards and visualization |
| `prometheus` | 9090 | Metrics collection |
| `tempo` | 3200 | Distributed tracing |
| `loki` | 3100 | Log aggregation |
| `otel-collector` | 4317, 4318 | OpenTelemetry collector |

### Application Services

| Service | Port | Description |
|---------|------|-------------|
| `graphql-gateway` | 5443 | GraphQL federation gateway (HTTPS) |
| `user-api` | 5300 | User/Client/Subscriber API |
| `product-api` | 5200 | Plans/Pricing API |
| `payment-api` | 5400 | Payments/Checkout API |
| `auth-api` | 5555 | Authentication API (HTTPS) |
| `webhooks` | 7071 | Stripe/Twilio webhook handler |
| `warp-cache` | 7777 | gRPC caching service |
| `localization-api` | 8888 | i18n string service |
| `graph-monitor` | 5145 | GraphQL schema registry |

### Kafka Consumers

All consumers use the same Docker image with different commands:

| Container | Topic | Description |
|-----------|-------|-------------|
| `notifications-listener` | Notifications | SMS delivery |
| `plans-listener` | Plans | Stripe product sync |
| `price-tiers-listener` | PriceTiers | Stripe price sync |
| `clients-listener` | Clients | Customer creation |
| `payments-listener` | Payments | Payment processing |
| `plan-subscription-listener` | PlanSubscriptions | Subscription lifecycle |
| `user-authentication-listener` | UserAuthentication | Login events |
| `stripe-webhooks-listener` | StripeProducts | External product sync |

### Web UIs

| Service | Port | Description |
|---------|------|-------------|
| `new-admin` | 5050 | Admin dashboard (HTTPS) |
| `new-signup` | 6060 | Subscriber signup site (HTTPS) |

### External Service Proxies

| Service | Port | Description |
|---------|------|-------------|
| `ngrok` | 4040 | Tunnel for webhooks |
| `stripe-cli` | - | Forwards Stripe webhooks |
| `payment-processor-proxy` | 4243 | Local Stripe API mock |
| `maildev` | 1080 | Email testing UI |

## Dockerfile Conventions

Dockerfiles live in the `dotnet/` directory, named after their service:

```
dotnet/
├── AuthApi.Dockerfile
├── AuthInit.Dockerfile
├── DbSeeder.Dockerfile
├── GraphMonitor.Dockerfile
├── GraphQLGateway.Dockerfile
├── KafkaConsumer.Dockerfile
├── LocalizationApi.Dockerfile
├── Migrator.Dockerfile
├── Nudges.Webhooks.Dockerfile
├── PaymentApi.Dockerfile
├── ProductApi.Dockerfile
├── UserApi.Dockerfile
└── WarpCache.Dockerfile
```

This placement allows Dockerfiles to reference shared projects without excessive `../` paths.

## Environment Files

Each service has its own `.env.docker` file:

```
dotnet/
├── AuthApi/.env.docker
├── ProductApi/.env.docker
├── UserApi/.env.docker
├── PaymentApi/.env.docker
├── KafkaConsumer/.env.docker
├── GraphQLGateway/GraphQLGateway/.env.docker
└── ...
```

Generate these with:

```powershell
./configure.ps1 -Mode Docker
```

## Running Individual Services

Start specific services:

```powershell
docker compose up postgres redis kafka -d
docker compose up user-api product-api -d
```

Rebuild a single service:

```powershell
docker compose build user-api
docker compose up user-api -d
```

View logs:

```powershell
docker compose logs -f user-api
docker compose logs -f plans-listener
```

## Resource Limits

All services have memory limits defined:

| Category | Memory |
|----------|--------|
| Infrastructure (Kafka, Postgres, Keycloak) | 1GB |
| APIs and Gateway | 512MB |
| Kafka Consumers | 256MB |
| Web UIs | 256MB |
| Utilities (ngrok, stripe-cli) | 128MB |

## Network

All services join the `Nudges` bridge network and communicate using container hostnames (e.g., `kafka:29092`, `postgres:5432`).

Services that need to reach the host machine (for Keycloak) use:

```yaml
extra_hosts:
  - "keycloak.local:host-gateway"
```

## Volumes

Certificate mounts for HTTPS services:

```yaml
volumes:
  - ./certs/aspnetapp.pfx:/https/aspnetapp.pfx:ro
  - ./certs/aspnetapp.crt:/etc/nginx/certs/aspnetapp.crt:ro
  - ./certs/aspnetapp.key:/etc/nginx/certs/aspnetapp.key:ro
```

## Healthchecks

Critical services have healthchecks:

- **Keycloak**: TCP check on port 9000
- **LocalizationApi**: TCP check on port 8888

Other services rely on Docker's restart policy.

## Secrets

The `graph-monitor-headers` secret is used during API builds to register schemas:

```yaml
secrets:
  graph-monitor-headers:
    file: ./dotnet/GraphMonitor/headers
```
