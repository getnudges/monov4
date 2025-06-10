# Use .NET 9 SDK as the base image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /app

# Copy project and restore dependencies
COPY ./Precision.WarpCache/Precision.WarpCache.csproj Precision.WarpCache/
COPY ./Precision.WarpCache.MemoryCache/Precision.WarpCache.MemoryCache.csproj Precision.WarpCache.MemoryCache/
COPY ./Precision.WarpCache.Grpc.Client/Precision.WarpCache.Grpc.Client.csproj Precision.WarpCache.Grpc.Client/
COPY ./Precision.WarpCache.Demo/Precision.WarpCache.Demo.csproj ./
RUN dotnet restore

# Copy everything else and build in release mode with AOT
COPY . ./
RUN dotnet publish -c Release -o /out --self-contained -p:PublishAot=true

# Runtime image for execution
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0 AS runtime
WORKDIR /app
COPY --from=build /out .

# Default entrypoint (can be overridden in docker-compose)
ENTRYPOINT ["./Precision.WarpCache.Demo"]
