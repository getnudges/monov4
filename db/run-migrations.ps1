dotnet ef database update `
    --context $args[0] `
    --project ../dotnet/UnAd.Data/UnAd.Data.csproj `
    --startup-project ../dotnet/tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj `
    --verbose
