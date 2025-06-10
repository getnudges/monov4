dotnet ef migrations add $args[0] `
    --context PaymentDbContext `
    --output-dir ../UnAd.Data/Payments/Migrations `
    --project ../dotnet/UnAd.Data/UnAd.Data.csproj `
    --startup-project ../dotnet/tools/UnAd.Data.Migrator/UnAd.Data.Migrator.csproj `
    --verbose
