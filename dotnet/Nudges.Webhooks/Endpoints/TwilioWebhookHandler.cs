using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Text;
using ErrorOr;
using Monads;
using Twilio.TwiML;
using Nudges.Localization.Client;
using Nudges.Webhooks.GraphQL;
using Nudges.Webhooks.Twilio;

namespace Nudges.Webhooks.Endpoints;

public partial class TwilioWebhookHandler(INudgesClient nudgesClient,
                                          TwilioMessageProcessor messageProcessor,
                                          ILocalizationClient localizer) {

    private static readonly ActivitySource ActivitySource = new($"{typeof(TwilioWebhookHandler).FullName}");
    private static readonly Meter Meter = new($"{typeof(TwilioWebhookHandler).FullName}");
    private static readonly Counter<long> WebhooksProcessed = Meter.CreateCounter<long>("webhooks_processed_total");
    private static readonly Counter<long> WebhooksReceived = Meter.CreateCounter<long>("webhooks_reveived_total");

    private static readonly string[] IgnoreList = [
        "stop",
        "start",
        "unstop",
        "help",
    ];

    private static Dictionary<string, string> ParseQueryString(string query) =>
        query.Split('&').ToDictionary(
            pair => WebUtility.UrlDecode(pair[..pair.IndexOf('=')]),
            pair => WebUtility.UrlDecode(pair[(pair.IndexOf('=') + 1)..]));

    public async Task<IResult> Endpoint(HttpRequest request, CancellationToken cancellationToken) {
        using var activity = ActivitySource.CreateActivity("TODO", ActivityKind.Consumer, request.GetActivityContext());
        using var streamReader = new StreamReader(request.Body);
        var body = await streamReader.ReadToEndAsync(cancellationToken);
        var form = ParseQueryString(body);
        var smsBody = form["Body"];
        var smsFrom = form["From"];

        if (IgnoreList.Any(i => i.Equals(smsBody.Trim(), StringComparison.OrdinalIgnoreCase))) {
            return Results.Ok();
        }

        WebhooksReceived.Add(1, [
            new("body", smsBody)
        ]);

        activity?.Start();

        var localeResult = await nudgesClient.SmsLocaleLookup(smsFrom, cancellationToken);
        
        if (localeResult.IsError) {
            var firstError = localeResult.FirstError;
            if (firstError.Type == ErrorType.NotFound) {
                var message = await localizer.GetLocalizedStringAsync("NotCustomer", CultureInfo.CurrentCulture.Name);
                return new MessagingResponse().Message(message).ToTextResult();
            }
            activity?.SetStatus(ActivityStatusCode.Error, firstError.Description);
            WebhooksProcessed.Add(1, [new("status", "error")]);
            return Results.Problem(firstError.Description);
        }

        var locale = localeResult.Value;
        var processResult = await messageProcessor.ProcessEvent(smsBody, smsFrom, locale, cancellationToken);

        try {
            return await processResult.Match<IResult>(result => {
                activity?.SetStatus(ActivityStatusCode.Ok);
                WebhooksProcessed.Add(1, [new("status", "success")]);
                return result.ToTextResult();
            }, async ex => {
                if (ex is NotCustomerException) {
                    var message = await localizer.GetLocalizedStringAsync("NotCustomer", CultureInfo.CurrentCulture.Name);
                    return new MessagingResponse().Message(message).ToTextResult();
                }
                if (ex is MessageException) {
                    var message = await localizer.GetLocalizedStringAsync("MessageException", CultureInfo.CurrentCulture.Name, new Dictionary<string, string> {
                        {"error", ex.Message}
                    });
                    return new MessagingResponse().Message(message).ToTextResult();
                }
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                WebhooksProcessed.Add(1, [new("status", "generic_error")]);
                return Results.Problem(ex.Message);
            });
        } catch (Exception ex) {
            return Results.Problem(ex.Message);
        }
    }
}

internal static class ResultHelpers {
    public static IResult ToTextResult(this MessagingResponse response) =>
        Results.Text(response.ToString(), System.Net.Mime.MediaTypeNames.Text.Xml, Encoding.UTF8);
}
