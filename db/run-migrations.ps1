dotnet ef database update `
    --context $args[0] `
    --project ../dotnet/Nudges.Data/Nudges.Data.csproj `
    --startup-project ../dotnet/tools/Nudges.Data.Migrator/Nudges.Data.Migrator.csproj `
    --verbose
