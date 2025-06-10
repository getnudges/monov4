FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src
COPY KafkaConsumer.Docker.sln ./
COPY Nudges.Models/*.csproj ./Nudges.Models/
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY Nudges.Kafka.Analyzers/*.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.Kafka/*.csproj ./Nudges.Kafka/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Localization/*.csproj ./Nudges.Localization/
COPY Nudges.Localization.Client/*.csproj ./Nudges.Localization.Client/
COPY Nudges.Localization.Analyzers/*.csproj ./Nudges.Localization.Analyzers/
COPY Monads/*.csproj ./Monads/
COPY Nudges.Stripe/*.csproj ./Nudges.Stripe/
COPY Nudges.Telemetry/*.csproj ./Nudges.Telemetry/
COPY Nudges.HotChocolate.Utils/*.csproj ./Nudges.HotChocolate.Utils/
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



