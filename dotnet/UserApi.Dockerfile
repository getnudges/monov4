# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src
COPY UserApi/*.csproj ./UserApi/
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

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore ./UserApi/UserApi.csproj

COPY UserApi/ ./UserApi/
COPY Nudges.Data/ ./Nudges.Data/
COPY Nudges.Models/ ./Nudges.Models/
COPY Monads/ ./Monads/
COPY Nudges.Redis/ ./Nudges.Redis/
COPY Nudges.Auth.Web/ ./Nudges.Auth.Web/
COPY Nudges.Kafka/ ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/ ./Nudges.Kafka.Analyzers/
COPY Nudges.Auth/ ./Nudges.Auth/
COPY Nudges.Telemetry/ ./Nudges.Telemetry/
COPY Nudges.Core/ ./Nudges.Core/
COPY Nudges.HotChocolate.Utils/ ./Nudges.HotChocolate.Utils/
COPY Precision.WarpCache/Precision.WarpCache/ ./Precision.WarpCache/Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/ ./Precision.WarpCache/Precision.WarpCache.Grpc.Client/
COPY Nudges.Configuration/ ./Nudges.Configuration/
COPY Nudges.Configuration.Analyzers/ ./Nudges.Configuration.Analyzers/
COPY Nudges.Security/ ./Nudges.Security/
COPY Nudges.Contracts/ ./Nudges.Contracts/
COPY Nudges.Kafka.Events/ ./Nudges.Kafka.Events/
COPY Nudges.Data.Security/ ./Nudges.Data.Security/

WORKDIR /src/UserApi
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build UserApi.csproj -c Release -o /app

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish UserApi.csproj -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "UserApi.dll"]



