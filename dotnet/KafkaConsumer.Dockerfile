# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY KafkaConsumer.Docker.sln ./

COPY KafkaConsumer/KafkaConsumer.csproj ./KafkaConsumer/
COPY Nudges.Kafka/Nudges.Kafka.csproj ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/Nudges.Kafka.Analyzers.csproj ./Nudges.Kafka.Analyzers/
COPY Nudges.Kafka.Events/Nudges.Kafka.Events.csproj ./Nudges.Kafka.Events/
COPY Nudges.Redis/Nudges.Redis.csproj ./Nudges.Redis/
COPY Nudges.Localization/Nudges.Localization.csproj ./Nudges.Localization/
COPY Monads/Monads.csproj ./Monads/
COPY Nudges.Stripe/Nudges.Stripe.csproj ./Nudges.Stripe/
COPY Nudges.Configuration/Nudges.Configuration.csproj ./Nudges.Configuration/
COPY Nudges.Configuration.Analyzers/Nudges.Configuration.Analyzers.csproj ./Nudges.Configuration.Analyzers/
COPY Precision.WarpCache/Precision.WarpCache.Grpc.Client/Precision.WarpCache.Grpc.Client.csproj ./Precision.WarpCache/Precision.WarpCache.Grpc.Client/
COPY Precision.WarpCache/Precision.WarpCache.MemoryCache/Precision.WarpCache.MemoryCache.csproj ./Precision.WarpCache/Precision.WarpCache.MemoryCache/
COPY Precision.WarpCache/Precision.WarpCache/Precision.WarpCache.csproj ./Precision.WarpCache/Precision.WarpCache/
COPY Nudges.Auth.Web/Nudges.Auth.Web.csproj ./Nudges.Auth.Web/
COPY Nudges.Auth/Nudges.Auth.csproj ./Nudges.Auth/
COPY Nudges.Localization.Client/Nudges.Localization.Client.csproj ./Nudges.Localization.Client/
COPY Nudges.Models/Nudges.Models.csproj ./Nudges.Models/

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore KafkaConsumer.Docker.sln

COPY KafkaConsumer/ ./KafkaConsumer/
COPY Nudges.Kafka/ ./Nudges.Kafka/
COPY Nudges.Kafka.Analyzers/ ./Nudges.Kafka.Analyzers/
COPY Nudges.Kafka.Events/ ./Nudges.Kafka.Events/
COPY Nudges.Redis/ ./Nudges.Redis/
COPY Nudges.Localization/ ./Nudges.Localization/
COPY Monads/ ./Monads/
COPY Nudges.Stripe/ ./Nudges.Stripe/
COPY Nudges.Configuration/ ./Nudges.Configuration/
COPY Nudges.Configuration.Analyzers/ ./Nudges.Configuration.Analyzers/
COPY Precision.WarpCache/ ./Precision.WarpCache/
COPY Nudges.Auth.Web/ ./Nudges.Auth.Web/
COPY Nudges.Auth/ ./Nudges.Auth/
COPY Nudges.Localization.Client/ ./Nudges.Localization.Client/
COPY Nudges.Models/ ./Nudges.Models/

WORKDIR /src/KafkaConsumer

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish KafkaConsumer.csproj -c Release -o /app /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "listener.dll"]
