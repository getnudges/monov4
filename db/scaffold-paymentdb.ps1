dotnet ef dbcontext scaffold `
    Name=ConnectionStrings:PaymentDb `
    --context PaymentDbContext `
    --namespace Nudges.Data.Payments.Models `
    --output-dir ../Nudges.Data/Payments/Models `
    --context-dir ../Nudges.Data/Payments `
    --context-namespace Nudges.Data.Payments `
    --project ../dotnet/Nudges.Data/Nudges.Data.csproj `
    --startup-project ../dotnet/tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj `
    --no-onconfiguring `
    --force `
    --verbose `
    Npgsql.EntityFrameworkCore.PostgreSQL
