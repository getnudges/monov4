# use bake to speed up builds
$env:COMPOSE_BAKE = "true"

# Generate environment files if they don't exist
./configure.ps1 -Docker
docker-compose up -d postgres

# keycloak (we need to wait so the configure script can connect)
docker-compose up -d --wait keycloak

# Generate Keycloak secrets
./configure.ps1 -Oidc

docker-compose run --rm --entrypoint="/app/migrateUserDb" db-migrator
docker-compose run --rm --entrypoint="/app/migrateProductDb" db-migrator
docker-compose run --rm --entrypoint="/app/migratePaymentDb" db-migrator

docker-compose run --rm auth-init

docker-compose run --rm db-seeder

# set up the base services
docker-compose up -d redis
docker-compose up -d warp-cache
docker-compose up -d localizer-api

docker-compose up -d kafka
docker-compose run --rm kafka-init-topics

docker-compose up -d auth-api

# setup the kafka listeners
# docker-compose up -d notifications-listener
# docker-compose up -d payments-listener
# docker-compose up -d clients-listener
docker-compose up -d plans-listener
# docker-compose up -d plan-subscription-listener
# docker-compose up -d price-tiers-listener
# docker-compose up -d user-authentication-listener
docker-compose up -d foreign-products-listener

# # setup the subgraphs for the gateway
docker-compose up -d user-api
# docker-compose up -d payment-api
docker-compose up -d product-api

# start the graph-monitor
docker-compose up -d graph-monitor

# # setup the gateway
$headers = Get-Content -Path ./dotnet/GraphMonitor/headers | ForEach-Object { "--header", $_ }
curl -sSf @headers http://localhost:5145/user-api -d "http://host.docker.internal:5300/graphql"
curl -sSf @headers http://localhost:5145/product-api -d "http://host.docker.internal:5200/graphql"
curl -sSf @headers http://localhost:5145/payment-api -d "http://host.docker.internal:5400/graphql"
docker-compose up -d graphql-gateway

# setup the proxy to stripe
docker-compose up -d payment-processor-proxy

# setup the webhook handler
docker-compose up -d ngrok
docker-compose up -d webhooks

# setup the UIs
docker-compose up -d new-admin
# docker-compose up -d new-signup

# # OTEL stuff
docker-compose up -d grafana

Write-Host "🚀 Done!"
Write-Host "To configure local development, run './configure.ps1 -Local'"
