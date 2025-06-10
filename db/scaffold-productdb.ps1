dotnet ef dbcontext scaffold `
    Name=ConnectionStrings:ProductDb `
    --context ProductDbContext `
    --namespace UnAd.Data.Products.Models `
    --output-dir ../UnAd.Data/Products/Models `
    --context-dir ../UnAd.Data/Products `
    --context-namespace UnAd.Data.Products `
    --project ../dotnet/UnAd.Data/UnAd.Data.csproj `
    --startup-project ../dotnet/tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj `
    --no-onconfiguring `
    --force `
    --verbose `
    Npgsql.EntityFrameworkCore.PostgreSQL
