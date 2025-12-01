FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src

COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Models/*.csproj ./Nudges.Models/
COPY Monads/*.csproj ./Monads/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY Nudges.Kafka/*.csproj ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/*.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Telemetry/*.csproj ./Nudges.Telemetry/
COPY Nudges.Core/*.csproj ./Nudges.Core/
COPY Nudges.HotChocolate.Utils/*.csproj ./Nudges.HotChocolate.Utils/
COPY Precision.WarpCache/Precision.WarpCache/*.csproj ./Precision.WarpCache/Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/*.csproj ./Precision.WarpCache/Precision.WarpCache.Grpc.Client/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/
COPY Nudges.Configuration.Analyzers/*.csproj ./Nudges.Configuration.Analyzers/
COPY Nudges.Security/*.csproj ./Nudges.Security/
COPY Nudges.Data.Security/*.csproj ./Nudges.Data.Security/
COPY Nudges.Contracts/*.csproj ./Nudges.Contracts/
COPY Nudges.Kafka.Events/*.csproj ./Nudges.Kafka.Events/
COPY UserApi/*.csproj ./UserApi/
COPY ProductApi/*.csproj ./ProductApi/
COPY PaymentApi/*.csproj ./PaymentApi/
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

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "GraphQLGateway.dll"]



