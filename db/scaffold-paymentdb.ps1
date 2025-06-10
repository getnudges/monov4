dotnet ef dbcontext scaffold `
    Name=ConnectionStrings:PaymentDb `
    --context PaymentDbContext `
    --namespace UnAd.Data.Payments.Models `
    --output-dir ../UnAd.Data/Payments/Models `
    --context-dir ../UnAd.Data/Payments `
    --context-namespace UnAd.Data.Payments `
    --project ../dotnet/UnAd.Data/UnAd.Data.csproj `
    --startup-project ../dotnet/tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj `
    --no-onconfiguring `
    --force `
    --verbose `
    Npgsql.EntityFrameworkCore.PostgreSQL
