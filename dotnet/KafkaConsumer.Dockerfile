FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src
COPY KafkaConsumer.Docker.sln ./
COPY UnAd.Models/*.csproj ./UnAd.Models/
COPY UnAd.Data/*.csproj ./UnAd.Data/
COPY UnAd.Redis/*.csproj ./UnAd.Redis/
COPY UnAd.Auth.Web/*.csproj ./UnAd.Auth.Web/
COPY UnAd.Kafka.Analyzers/*.csproj ./UnAd.Kafka.Analyzers/
COPY UnAd.Kafka/*.csproj ./UnAd.Kafka/
COPY UnAd.Auth/*.csproj ./UnAd.Auth/
COPY UnAd.Localization/*.csproj ./UnAd.Localization/
COPY UnAd.Localization.Client/*.csproj ./UnAd.Localization.Client/
COPY UnAd.Localization.Analyzers/*.csproj ./UnAd.Localization.Analyzers/
COPY Monads/*.csproj ./Monads/
COPY UnAd.Stripe/*.csproj ./UnAd.Stripe/
COPY UnAd.Telemetry/*.csproj ./UnAd.Telemetry/
COPY UnAd.HotChocolate.Utils/*.csproj ./UnAd.HotChocolate.Utils/
COPY Precision.WarpCache/Precision.WarpCache/*.csproj ./Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.MemoryCache/*.csproj ./Precision.WarpCache.MemoryCache/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/*.csproj ./Precision.WarpCache.Grpc.Client/
COPY KafkaConsumer/*.csproj ./KafkaConsumer/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/

RUN dotnet restore
COPY . .
WORKDIR /src/KafkaConsumer
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish KafkaConsumer.csproj -c Release -o /src/publish /p:UseAppHost=false /p:PublishReadyToRun=true

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENTRYPOINT ["dotnet", "listener.dll"]
# throw listener name on docker-compose



