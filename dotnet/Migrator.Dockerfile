FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

# Install Kerberos library
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
    libgssapi-krb5-2

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY Nudges.Data.Migrator.sln ./Nudges.Data.Migrator.sln
COPY tools/Nudges.Data.Migrator/*.csproj ./tools/Nudges.Data.Migrator/
COPY Nudges.Data/*.csproj ./Nudges.Data/
COPY Nudges.Security/*.csproj ./Nudges.Security/
COPY Nudges.Data.Security/*.csproj ./Nudges.Data.Security/

RUN dotnet restore

COPY Nudges.Data.Migrator.sln ./Nudges.Data.Migrator.sln
COPY tools/Nudges.Data.Migrator/ ./tools/Nudges.Data.Migrator/
COPY Nudges.Data/ ./Nudges.Data/
COPY Nudges.Security/ ./Nudges.Security/
COPY Nudges.Data.Security/ ./Nudges.Data.Security/

COPY .config/ ./.config

RUN dotnet tool restore

RUN dotnet ef migrations bundle \
    --project ./Nudges.Data/Nudges.Data.csproj \
    --startup-project ./tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context UserDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migrateUserDb

RUN dotnet ef migrations bundle \
    --project ./Nudges.Data/Nudges.Data.csproj \
    --startup-project ./tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context ProductDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migrateProductDb

RUN dotnet ef migrations bundle \
    --project ./Nudges.Data/Nudges.Data.csproj \
    --startup-project ./tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context PaymentDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migratePaymentDb

FROM base AS exec

WORKDIR /app
COPY --from=build /src/migrateUserDb migrateUserDb
COPY --from=build /src/migrateProductDb migrateProductDb
COPY --from=build /src/migratePaymentDb migratePaymentDb
