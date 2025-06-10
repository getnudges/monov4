#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Install clang/zlib1g-dev dependencies for publishing to native
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev
WORKDIR /src

COPY Precision.WarpCache/Precision.WarpCache/*.csproj ./Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Redis/*.csproj ./Precision.WarpCache.Redis/
COPY GraphMonitor/GraphMonitor.csproj .

RUN dotnet restore  GraphMonitor.csproj

COPY Precision.WarpCache/ /Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Redis/ ./Precision.WarpCache.Redis/
COPY ./GraphMonitor .

WORKDIR "/src/."
RUN dotnet build "./GraphMonitor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "./GraphMonitor.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /app/publish \
    /p:PublishAot=true \
    /p:InvariantGlobalization=true

FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["./GraphMonitor"]
