# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["./LocalizationApi/LocalizationApi.csproj", "."]
RUN dotnet restore "LocalizationApi.csproj"

# Copy the rest of the source code
COPY ./LocalizationApi/ .

# Install NativeAOT build prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    clang zlib1g-dev

# Build and publish the app with AOT enabled
RUN dotnet publish "LocalizationApi.csproj" \
    -r linux-x64 \
    -c Release \
    -o /app/publish \
    /p:PublishAot=true \
    /p:InvariantGlobalization=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0 AS runtime
WORKDIR /app

# Install ICU libraries for proper localization support
RUN apt-get update \
    && apt-get install -y libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# Set environment variables for localization
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV LC_ALL=en_US.UTF-8
ENV LANG=en_US.UTF-8

# # Create non-root user for security
# RUN adduser --disabled-password --gecos "" appuser
# USER appuser

# # Copy the published AOT app from the build stage
COPY --from=build /app/publish .

# Configure health checks
# HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
#     CMD wget -q -O - http://localhost:8888/health || exit 1

ENTRYPOINT ["/app/LocalizationApi"]

