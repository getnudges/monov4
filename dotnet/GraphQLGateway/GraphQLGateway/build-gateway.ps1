$ErrorActionPreference = "Stop"

dotnet fusion subgraph config set name "UserApi" -w "../../UserApi" -c "../../UserApi/subgraph-config.json"
dotnet fusion subgraph config set http --url "http://localhost:5300/graphql" -w ../../UserApi -c ../../UserApi/subgraph-config.json
dotnet run --project ../../UserApi/UserApi.csproj -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ../../UserApi

dotnet fusion subgraph config set name "ProductApi" -w "../../ProductApi" -c "../../ProductApi/subgraph-config.json"
dotnet fusion subgraph config set http --url "http://localhost:5200/graphql" -w ../../ProductApi -c ../../ProductApi/subgraph-config.json
dotnet run --project ../../ProductApi/ProductApi.csproj -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ../../ProductApi

dotnet fusion subgraph config set name "PaymentApi" -w "../../PaymentApi" -c "../../PaymentApi/subgraph-config.json"
dotnet fusion subgraph config set http --url "http://localhost:5400/graphql" -w ../../PaymentApi -c ../../PaymentApi/subgraph-config.json
dotnet run --project ../../PaymentApi/PaymentApi.csproj -- schema export --output schema.graphql
dotnet fusion subgraph pack -w ../../PaymentApi

dotnet fusion compose -p gateway -s ../../UserApi
dotnet fusion compose -p gateway -s ../../ProductApi
dotnet fusion compose -p gateway -s ../../PaymentApi

dotnet run -- schema export --output schema.graphql
