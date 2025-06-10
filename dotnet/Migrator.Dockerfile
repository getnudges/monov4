ARG ConnectionStrings__UserDb
ARG ConnectionStrings__ProductDb
ARG ConnectionStrings__PaymentDb

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

COPY ./UnAd.Data.Migrator.sln ./UnAd.Data.Migrator.sln
COPY ./tools/UnAd.Data.Migrator/ ./tools/UnAd.Data.Migrator/
COPY ./UnAd.Data/ ./UnAd.Data/

RUN dotnet restore

COPY .config/ ./.config

RUN dotnet tool restore

RUN dotnet ef migrations bundle \
    --project ./UnAd.Data/UnAd.Data.csproj \
    --startup-project ./tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context UserDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migrateUserDb

RUN dotnet ef migrations bundle \
    --project ./UnAd.Data/UnAd.Data.csproj \
    --startup-project ./tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context ProductDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migrateProductDb

RUN dotnet ef migrations bundle \
    --project ./UnAd.Data/UnAd.Data.csproj \
    --startup-project ./tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj \
    --self-contained -r linux-x64 \
    --context PaymentDbContext

RUN chmod +x ./efbundle
RUN mv ./efbundle ./migratePaymentDb

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS exec

ENV ConnectionStrings__UserDb="$ConnectionStrings__UserDb"
ENV ConnectionStrings__ProductDb="$ConnectionStrings__ProductDb"
ENV ConnectionStrings__PaymentDb="$ConnectionStrings__PaymentDb"

WORKDIR /app
COPY --from=build /src/migrateUserDb migrateUserDb
COPY --from=build /src/migrateProductDb migrateProductDb
COPY --from=build /src/migratePaymentDb migratePaymentDb
