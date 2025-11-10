# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY *.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore

COPY . .
WORKDIR /src/AuthApi

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish AuthApi.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENTRYPOINT ["dotnet", "AuthApi.dll"]



