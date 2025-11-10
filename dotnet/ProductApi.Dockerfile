# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src

COPY *.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore

COPY . .
WORKDIR /src/ProductApi
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet build -c Release -o /app

FROM build AS publish
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish ProductApi.csproj -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "ProductApi.dll"]



