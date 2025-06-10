FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG GRAPH_MONITOR_URL

WORKDIR /src
COPY ProductApi.Docker.sln ./
COPY Monads/*.csproj ./Monads/
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Models/*.csproj ./Nudges.Models/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY Nudges.Kafka/*.csproj ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/*.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Telemetry/*.csproj ./Nudges.Telemetry/
COPY Nudges.HotChocolate.Utils/*.csproj ./Nudges.HotChocolate.Utils/
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



