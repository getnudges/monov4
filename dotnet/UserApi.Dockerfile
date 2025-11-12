# syntax=docker/dockerfile:1
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
COPY Nudges.HotChocolate.Utils/*.csproj ./Nudges.HotChocolate.Utils/
COPY UserApi/*.csproj ./UserApi/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/*.csproj ./Precision.WarpCache.Grpc.Client/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore ./UserApi/UserApi.csproj
COPY . .
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



