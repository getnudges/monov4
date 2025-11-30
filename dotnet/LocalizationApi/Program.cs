using System.Globalization;
using LocalizationApi;
using LocalizationApi.Services;
using Microsoft.AspNetCore.Localization;
using Nudges.Telemetry;

var builder = WebApplication.CreateBuilder(args);

var settings = new Settings();
builder.Configuration.Bind(settings);

builder.Services.AddLogging(configure => configure
        .AddSimpleConsole(o =>
            o.SingleLine = !builder.Environment.IsDevelopment()));

builder.Services.AddGrpc(o => o.EnableDetailedErrors = true);

if (settings.Otlp.Endpoint is string url) {

    builder.Services.AddOpenTelemetryConfiguration<Program>(
            url,
            builder.Environment.ApplicationName, [
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "System.Net.Http",
            ], [], null, null, options => options.RecordException = true);
}

builder.Services.AddHealthChecks();

builder.Services.AddLocalization();

var app = builder.Build();

app.UseHealthChecks("/");

var supportedCultures = new[] {
    new CultureInfo("en-US"),
    new CultureInfo("es-ES")
};

app.UseRequestLocalization(new RequestLocalizationOptions {
    DefaultRequestCulture = new RequestCulture("en-US"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

app.MapGrpcService<LocalizationService>();

if (settings.Otlp.Endpoint is not null) {
    app.MapPrometheusScrapingEndpoint();
}

app.Run();
