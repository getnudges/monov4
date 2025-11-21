FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Debug

WORKDIR /src

COPY . .
RUN dotnet restore ./Nudges.Webhooks.sln

FROM build AS final
WORKDIR /src

ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV ASPNETCORE_HTTP_PORTS=7071
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "watch", "--project", "Nudges.Webhooks/Nudges.Webhooks.csproj", "--configuration", "Debug", "--no-launch-profile", "--urls", "http://+:7071"]



