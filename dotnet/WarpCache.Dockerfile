FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

WORKDIR /src

COPY Precision.WarpCache/Precision.WarpCache/Precision.WarpCache.csproj ./Precision.WarpCache/Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Grpc/Precision.WarpCache.Grpc.csproj ./Precision.WarpCache/Precision.WarpCache.Grpc/
COPY Precision.WarpCache/Precision.WarpCache.MemoryCache/Precision.WarpCache.MemoryCache.csproj ./Precision.WarpCache/Precision.WarpCache.MemoryCache/
COPY Precision.WarpCache/Precision.WarpCache.Redis/Precision.WarpCache.Redis.csproj ./Precision.WarpCache/Precision.WarpCache.Redis/
COPY Nudges.Configuration/Nudges.Configuration.csproj ./Nudges.Configuration/
COPY Nudges.Telemetry/Nudges.Telemetry.csproj ./Nudges.Telemetry/

RUN dotnet restore "./Precision.WarpCache/Precision.WarpCache.Grpc/Precision.WarpCache.Grpc.csproj"

COPY Precision.WarpCache/Precision.WarpCache ./Precision.WarpCache/Precision.WarpCache/
COPY Precision.WarpCache/Precision.WarpCache.Grpc ./Precision.WarpCache/Precision.WarpCache.Grpc/
COPY Precision.WarpCache/Precision.WarpCache.MemoryCache ./Precision.WarpCache/Precision.WarpCache.MemoryCache/
COPY Precision.WarpCache/Precision.WarpCache.Redis ./Precision.WarpCache/Precision.WarpCache.Redis/
COPY Nudges.Configuration ./Nudges.Configuration/
COPY Nudges.Telemetry ./Nudges.Telemetry/

WORKDIR /src/Precision.WarpCache

# Build and publish the app with AOT enabled
RUN dotnet publish "Precision.WarpCache.Grpc/Precision.WarpCache.Grpc.csproj" \
    -r linux-x64 \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

# Copy the published AOT app from the build stage
COPY --from=build /app/publish .

ENTRYPOINT ["/app/Precision.WarpCache.Grpc"]

