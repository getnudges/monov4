FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY UnAd.Models/*.csproj ./UnAd.Models/
COPY UnAd.Redis/*.csproj ./UnAd.Redis/
COPY UnAd.Kafka/*.csproj ./UnAd.Kafka/
COPY UnAd.Kafka.Analyzers/*.csproj ./UnAd.Kafka.Analyzers/
COPY UnAd.Auth/*.csproj ./UnAd.Auth/
COPY UnAd.Auth.Web/*.csproj ./UnAd.Auth.Web/
COPY Monads/*.csproj ./Monads/
COPY UnAd.Localization/*.csproj ./UnAd.Localization/
COPY UnAd.Localization.Analyzers/*.csproj ./UnAd.Localization.Analyzers/
COPY UnAd.Configuration.Analyzers/*.csproj ./UnAd.Configuration.Analyzers/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/*.csproj Precision.WarpCache/Precision.WarpCache.Grpc.Client/
COPY Nudges.Configuration/*.csproj ./Nudges.Configuration/
COPY AuthApi/*.csproj ./AuthApi/

RUN dotnet restore AuthApi/AuthApi.csproj
COPY . .
WORKDIR /src/AuthApi
RUN dotnet build AuthApi.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish AuthApi.csproj -c Release -o /src/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /src/publish .

ENTRYPOINT ["dotnet", "AuthApi.dll"]



