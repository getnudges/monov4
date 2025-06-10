dotnet ef dbcontext scaffold `
    Name=ConnectionStrings:UserDb `
    --context UserDbContext `
    --namespace Nudges.Data.Users.Models `
    --output-dir ../Nudges.Data/Users/Models `
    --context-dir ../Nudges.Data/Users `
    --context-namespace Nudges.Data.Users `
    --project ../dotnet/Nudges.Data/Nudges.Data.csproj `
    --startup-project ../dotnet/tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj `
    --no-onconfiguring `
    --force `
    --verbose `
    Npgsql.EntityFrameworkCore.PostgreSQL
