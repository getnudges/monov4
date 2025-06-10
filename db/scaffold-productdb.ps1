dotnet ef dbcontext scaffold `
    Name=ConnectionStrings:ProductDb `
    --context ProductDbContext `
    --namespace Nudges.Data.Products.Models `
    --output-dir ../Nudges.Data/Products/Models `
    --context-dir ../Nudges.Data/Products `
    --context-namespace Nudges.Data.Products `
    --project ../dotnet/Nudges.Data/Nudges.Data.csproj `
    --startup-project ../dotnet/tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj `
    --no-onconfiguring `
    --force `
    --verbose `
    Npgsql.EntityFrameworkCore.PostgreSQL
