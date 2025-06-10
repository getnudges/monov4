FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src
COPY ProductApi.Docker.sln ./
COPY Monads/*.csproj ./Monads/
COPY UnAd.Data/*.csproj ./UnAd.Data/
COPY UnAd.Models/*.csproj ./UnAd.Models/
COPY UnAd.Redis/*.csproj ./UnAd.Redis/
COPY UnAd.Auth.Web/*.csproj ./UnAd.Auth.Web/
COPY UnAd.Kafka/*.csproj ./UnAd.Kafka/
COPY UnAd.Kafka.Analyzers/*.csproj ./UnAd.Kafka.Analyzers/
COPY UnAd.Auth/*.csproj ./UnAd.Auth/
COPY UnAd.Telemetry/*.csproj ./UnAd.Telemetry/
COPY UnAd.HotChocolate.Utils/*.csproj ./UnAd.HotChocolate.Utils/
COPY ProductApi/*.csproj ./ProductApi/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/

RUN dotnet restore
COPY . .
WORKDIR /src/ProductApi
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish ProductApi.csproj -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .
ENTRYPOINT ["dotnet", "ProductApi.dll"]



