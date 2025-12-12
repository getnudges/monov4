# Exit on any error
$ErrorActionPreference = "Stop"

# Helper function to exit on non-zero exit codes
function Invoke-CommandLine {
    param([scriptblock]$Command)
    & $Command
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

# use bake to speed up builds
$env:COMPOSE_BAKE = "true"

# Generate environment files if they don't exist
./configure.ps1 -Docker

# start the databases
docker compose up -d postgres
docker compose up -d redis

# keycloak (we need to wait so the configure script can connect)
docker compose up -d --wait keycloak

# Generate Keycloak secrets
./configure.ps1 -Oidc

# some startup services depend on WarpCache
docker compose up -d warp-cache

docker compose run --rm --entrypoint="/app/migrateUserDb" db-migrator
docker compose run --rm --entrypoint="/app/migrateProductDb" db-migrator
docker compose run --rm --entrypoint="/app/migratePaymentDb" db-migrator

Invoke-CommandLine { docker compose run --rm auth-init }

Invoke-CommandLine { docker compose run --rm db-seeder }

docker compose up -d kafka
docker compose run --rm kafka-init-topics

# set up the auth service
docker compose up -d auth-api

# set up the other base services
docker compose up -d localization-api

# setup the kafka listeners
docker compose up -d user-authentication-listener
docker compose up -d plans-listener
docker compose up -d notifications-listener
docker compose up -d stripe-webhooks-listener
# docker compose up -d payments-listener
# docker compose up -d clients-listener
# docker compose up -d plan-subscription-listener
# docker compose up -d price-tiers-listener

# setup the subgraphs for the gateway
docker compose up -d user-api
docker compose up -d payment-api
docker compose up -d product-api

# start the graph-monitor
docker compose up -d graph-monitor

# setup the gateway
$headers = Get-Content -Path ./dotnet/GraphMonitor/headers | ForEach-Object { "--header", $_ }
curl -sSf @headers http://localhost:5145/user-api -d "http://host.docker.internal:5300/graphql"
curl -sSf @headers http://localhost:5145/product-api -d "http://host.docker.internal:5200/graphql"
curl -sSf @headers http://localhost:5145/payment-api -d "http://host.docker.internal:5400/graphql"

# fail if the gateway can't build/run
Invoke-CommandLine { docker compose up -d graphql-gateway }

# setup the UIs
docker compose up -d new-admin
# docker compose up -d new-signup

# setup the proxy to Stripe
docker compose up -d payment-processor-proxy

# setup the webhook handler
docker compose up -d ngrok
docker compose up -d webhooks

# OTEL stuff
docker compose up -d grafana

Write-Host "🚀 Done!"
Write-Host "To configure local development, run './configure.ps1 -Local'"
