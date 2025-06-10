FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY . .
RUN dotnet restore ./UnAd.Webhooks/UnAd.Webhooks.csproj

RUN dotnet build ./UnAd.Webhooks/UnAd.Webhooks.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/UnAd.Webhooks
RUN dotnet publish "./UnAd.Webhooks.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UnAd.Webhooks.dll"]



