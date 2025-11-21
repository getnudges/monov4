# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Models/*.csproj ./Nudges.Models/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Telemetry/*.csproj ./Nudges.Telemetry/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY Nudges.Kafka/*.csproj ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/*.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.HotChocolate.Utils/*.csproj ./Nudges.HotChocolate.Utils/
COPY ProductApi/*.csproj ./ProductApi/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/
COPY Nudges.Telemetry/*.csproj ./Nudges.Telemetry/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore ProductApi/ProductApi.csproj
COPY . .
WORKDIR /src/ProductApi
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build ProductApi.csproj -c Release -o /app

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish ProductApi.csproj -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "ProductApi.dll"]



