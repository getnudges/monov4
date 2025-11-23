FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY . .
RUN dotnet restore ./Nudges.Webhooks/Nudges.Webhooks.csproj

RUN dotnet build ./Nudges.Webhooks/Nudges.Webhooks.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR /src/Nudges.Webhooks
RUN dotnet publish "./Nudges.Webhooks.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=true

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nudges.Webhooks.dll"]



