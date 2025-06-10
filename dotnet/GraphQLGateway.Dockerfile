FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src

COPY UnAd.Data/*.csproj ./UnAd.Data/
COPY UnAd.Redis/*.csproj ./UnAd.Redis/
COPY UnAd.Telemetry/*.csproj ./UnAd.Telemetry/
COPY Monads/*.csproj ./Monads/
COPY UnAd.Auth/*.csproj ./UnAd.Auth/
COPY UnAd.Kafka/*.csproj ./UnAd.Kafka/
COPY UnAd.Kafka.Analyzers/*.csproj ./UnAd.Kafka.Analyzers/
COPY UnAd.Auth.Web/*.csproj ./UnAd.Auth.Web/
COPY UnAd.HotChocolate.Utils/*.csproj ./UnAd.HotChocolate.Utils/
COPY UserApi/*.csproj ./UserApi/
COPY ProductApi/*.csproj ./ProductApi/
COPY PaymentApi/*.csproj ./PaymentApi/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/Precision.WarpCache.Grpc.Client.csproj ./Precision.WarpCache.Grpc.Client/
COPY GraphQLGateway/GraphQLGateway/*.csproj ./GraphQLGateway/GraphQLGateway/

RUN dotnet restore GraphQLGateway/GraphQLGateway/GraphQLGateway.csproj
COPY . .

RUN dotnet build GraphQLGateway/GraphQLGateway/GraphQLGateway.csproj -c Release -o /app/build

FROM build AS fusion

RUN dotnet tool restore

############################################################################################################
# TODO: So this is what we need:
#       1. When a subgraph is successfully deployed, we need to communicate it's graphql endpoint to
#          a centralized location. This will allow us to generate the subgraph config for that project.
#       2. When we come here to build the gateway, we need to pull in that subgraph config and use it
#          to generate the gateway schema.
############################################################################################################

RUN dotnet fusion subgraph config set name "UserApi" -w ./UserApi -c ./UserApi/subgraph-config.json
RUN --mount=type=secret,id=graph-monitor-headers,target=/run/secrets/headers \
    URL=$(curl -sSf -H @/run/secrets/headers ${GRAPH_MONITOR_URL}/user-api) && \
    dotnet fusion subgraph config set http --url "$URL" -w ./UserApi -c ./UserApi/subgraph-config.json

RUN dotnet fusion subgraph config set name "ProductApi" -w ./ProductApi -c ./ProductApi/subgraph-config.json
RUN --mount=type=secret,id=graph-monitor-headers,target=/run/secrets/headers \
    URL=$(curl -sSf -H @/run/secrets/headers ${GRAPH_MONITOR_URL}/product-api) && \
    dotnet fusion subgraph config set http --url "$URL" -w ./ProductApi -c ./ProductApi/subgraph-config.json

RUN dotnet fusion subgraph config set name "PaymentApi" -w ./PaymentApi -c ./PaymentApi/subgraph-config.json
RUN --mount=type=secret,id=graph-monitor-headers,target=/run/secrets/headers \
    URL=$(curl -sSf -H @/run/secrets/headers ${GRAPH_MONITOR_URL}/payment-api) && \
    dotnet fusion subgraph config set http --url "$URL" -w ./PaymentApi -c ./PaymentApi/subgraph-config.json

RUN ASPNETCORE_ENVIRONMENT=Development && dotnet run --project UserApi/UserApi.csproj -- schema export --output schema.graphql
RUN ASPNETCORE_ENVIRONMENT=Development && dotnet run --project ProductApi/ProductApi.csproj -- schema export --output schema.graphql
RUN ASPNETCORE_ENVIRONMENT=Development && dotnet run --project PaymentApi/PaymentApi.csproj -- schema export --output schema.graphql

RUN dotnet fusion subgraph pack -w ./UserApi
RUN dotnet fusion subgraph pack -w ./ProductApi
RUN dotnet fusion subgraph pack -w ./PaymentApi

RUN dotnet fusion compose -p ./GraphQLGateway/GraphQLGateway/gateway -s ./UserApi
RUN dotnet fusion compose -p ./GraphQLGateway/GraphQLGateway/gateway -s ./ProductApi
RUN dotnet fusion compose -p ./GraphQLGateway/GraphQLGateway/gateway -s ./PaymentApi

FROM fusion AS publish
WORKDIR /src/GraphQLGateway/GraphQLGateway/
RUN dotnet publish "GraphQLGateway.csproj" -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "GraphQLGateway.dll"]



