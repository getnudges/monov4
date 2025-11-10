# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY KafkaConsumer.Docker.sln ./
COPY *.csproj ./
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore

COPY . .
WORKDIR /src/KafkaConsumer

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish KafkaConsumer.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "KafkaConsumer.dll"]



