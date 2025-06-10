if ($PSVersionTable.Platform -eq 'Unix') {
    $env:COMPOSE_BAKE="true"
}
    
docker-compose up -d keycloak

# OTEL stuff
# docker-compose run --rm init-tempo
# docker-compose up -d tempo
# docker-compose up -d grafana

# base stuff everything depends on
docker-compose up -d postgres
docker-compose up -d redis
docker-compose up -d warp-cache

# TODO: wait for keycloak to be up and running

# & .\keycloak\generate-secrets.ps1

# docker-compose up -d unleash

# setup databases
docker-compose run --rm --entrypoint="/app/migrateUserDb" db-migrator
docker-compose run --rm --entrypoint="/app/migrateProductDb" db-migrator
docker-compose run --rm --entrypoint="/app/migratePaymentDb" db-migrator
docker-compose run --rm db-seeder

# setup the rest of the APIs
docker-compose up -d auth-api
docker-compose up -d localizer-api

# setup the subgraphs for the gateway
docker-compose up -d user-api
docker-compose up -d payment-api
docker-compose up -d product-api

# start the graph-monitor
docker-compose up -d graph-monitor

# setup the gateway
$headers = Get-Content -Path ./dotnet/GraphMonitor/headers | ForEach-Object { "--header", $_ }
curl -sSf @headers http://localhost:5145/user-api -d "http://host.docker.internal:5300/graphql"
curl -sSf @headers http://localhost:5145/product-api -d "http://host.docker.internal:5200/graphql"
curl -sSf @headers http://localhost:5145/payment-api -d "http://host.docker.internal:5400/graphql"
docker-compose up -d graphql-gateway

# setup kafka
docker-compose up -d zookeeper
docker-compose up -d kafka
docker-compose run --rm kafka-init-topics

# setup the kafka listeners
docker-compose up -d notifications-listener
docker-compose up -d payments-listener
docker-compose up -d clients-listener
docker-compose up -d plans-listener
docker-compose up -d plan-subscription-listener
docker-compose up -d price-tiers-listener

# setup the webhook handler
docker-compose up -d ngrok
docker-compose up -d webhooks

# setup the UIs
docker-compose up -d new-admin
docker-compose up -d new-signup
