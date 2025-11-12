ARG ConnectionStrings__UserDb
ARG ConnectionStrings__ProductDb
ARG ConnectionStrings__PaymentDb

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ./Nudges.Data.Migrator.sln ./Nudges.Data.Migrator.sln
COPY ./tools/Nudges.Data.Migrator/ ./tools/Nudges.Data.Migrator/
COPY ./Nudges.Data/ ./Nudges.Data/

RUN dotnet restore

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

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS exec

ENV ConnectionStrings__UserDb="$ConnectionStrings__UserDb"
ENV ConnectionStrings__ProductDb="$ConnectionStrings__ProductDb"
ENV ConnectionStrings__PaymentDb="$ConnectionStrings__PaymentDb"

WORKDIR /app
COPY --from=build /src/migrateUserDb migrateUserDb
COPY --from=build /src/migrateProductDb migrateProductDb
COPY --from=build /src/migratePaymentDb migratePaymentDb
