using System.Diagnostics;
using System.Diagnostics.Metrics;
using Monads;
using OpenTelemetry;
using Stripe;
using UnAd.Configuration.Extensions;
using UnAd.Webhooks.Stripe;

namespace UnAd.Webhooks.Endpoints;

public class StripeWebhookHandler(IStripeVerifier stripeVerifier,
                                  IConfiguration config,
                                  StripeMessageProcessor messageProcessor,
                                  ILogger<StripeWebhookHandler> logger) {

    private readonly string _stripeEndpointSecret = config.GetStripeWebhookEndpointSecret();

    private static readonly ActivitySource ActivitySource = new($"{typeof(StripeWebhookHandler).FullName}");
    private static readonly Meter Meter = new($"{typeof(StripeWebhookHandler).FullName}");
    private static readonly Counter<long> WebhooksProcessed = Meter.CreateCounter<long>("webhooks_processed_total");
    private static readonly Counter<long> WebhooksReceived = Meter.CreateCounter<long>("webhooks_reveived_total");
    private static readonly Histogram<double> WebhookProcessingTime = Meter.CreateHistogram<double>("webhooks_processing_time_seconds");

    private async Task<Result<Event, Exception>> VerifyRequest(HttpRequest request) {
        Event stripeEvent = default!;
        using var streamReader = new StreamReader(request.Body);
        var body = await streamReader.ReadToEndAsync();
        if (request.Headers.TryGetValue("stripe-signature", out var sig)) {
            return stripeVerifier.Verify(sig!, _stripeEndpointSecret, body);
        }
        return stripeEvent;
    }

    public async Task<IResult> Endpoint(HttpRequest request) {
        WebhooksReceived.Add(1, [new("status", "received")]);
        var eventResult = await VerifyRequest(request);

        if (Activity.Current?.Context.IsValid() == true) {
            request.HttpContext.Items["traceparent"] = Activity.Current.TraceId;
        }

        return await eventResult.Match<IResult>(async r => {
            var result = await HandleEvent(new(r, request), request.HttpContext.RequestAborted);
            return result.Match(Results.Ok(), e => {
                logger.LogException(e);
                return Results.Problem(e.Message);
            });
        }, e => {
            logger.LogException(e);
            return Results.BadRequest(e.Message);
        });
    }

    private async Task<Maybe<Exception>> HandleEvent(StripeEventContext context, CancellationToken cancellationToken) {

        using var activity = context.StripeEvent.Request.GetActivity(ActivitySource, $"HandleEvent_{context.StripeEvent.Type}");
        WebhooksReceived.Add(1, [new("type", $"stripe.{context.StripeEvent.Type}")]);
        activity?.SetTag("stripe.eventId", context.StripeEvent.Id);
        activity?.Start();

        var startTime = Stopwatch.GetTimestamp();
        try {
            logger.LogProcessingRequest(context.StripeEvent.Type);

            var result = await messageProcessor.ProcessEvent(context, cancellationToken);

            activity?.SetStatus(ActivityStatusCode.Ok);
            WebhooksProcessed.Add(1, [new("status", "success")]);
            return result;
        } catch (StripeException ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            WebhooksProcessed.Add(1, [new("status", "stripe_error")]);
            return ex;
        } catch (Exception ex) {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            WebhooksProcessed.Add(1, [new("status", "generic_error")]);
            return ex;
        } finally {
            var endTime = Stopwatch.GetTimestamp();
            var duration = (endTime - startTime) / (double)Stopwatch.Frequency;
            WebhookProcessingTime.Record(duration);
        }
    }
}
