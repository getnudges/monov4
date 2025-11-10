FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY Nudges.Models/*.csproj ./Nudges.Models/
COPY Nudges.Redis/*.csproj ./Nudges.Redis/
COPY Nudges.Kafka/*.csproj ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/*.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.Auth/*.csproj ./Nudges.Auth/
COPY Nudges.Auth.Web/*.csproj ./Nudges.Auth.Web/
COPY Monads/*.csproj ./Monads/
COPY Nudges.Localization/*.csproj ./Nudges.Localization/
COPY Nudges.Localization.Analyzers/*.csproj ./Nudges.Localization.Analyzers/
COPY Nudges.Configuration.Analyzers/*.csproj ./Nudges.Configuration.Analyzers/
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



