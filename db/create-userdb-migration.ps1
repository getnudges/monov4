dotnet ef migrations add $args[0] `
    --context UserDbContext `
    --output-dir ../Nudges.Data/Users/Migrations `
    --project ../dotnet/Nudges.Data/Nudges.Data.csproj `
    --startup-project ../dotnet/tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj `
    --verbose
