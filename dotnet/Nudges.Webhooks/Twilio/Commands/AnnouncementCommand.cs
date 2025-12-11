using System.Globalization;
using System.Text.RegularExpressions;
using ErrorOr;
using Monads;
using Precision.WarpCache.Grpc.Client;
using Twilio.TwiML;
using Nudges.Kafka;
using Nudges.Localization.Client;
using Nudges.Webhooks.GraphQL;
using Nudges.Webhooks.Twilio;
using Nudges.Webhooks.Twilio.Commands;
using Nudges.Kafka.Events;

namespace Nudges.Webhooks.Endpoints.Handlers;

internal sealed partial class AnnouncementCommand(INudgesClient nudgesClient,
                                                  ICacheClient<string> cache,
                                                  ILocalizationClient localizer,
                                                  KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer) : ITwilioEventCommand {

    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@".*", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();

    public async Task<Monads.Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) {
        var result = await nudgesClient.GetClientByPhoneNumber(context.From, cancellationToken);
        
        if (result.IsError) {
            return new GraphQLException(result.FirstError.Description);
        }

        var client = result.Value;
        if (client.Subscription?.Status != "ACTIVE") {
            return new NotCustomerException();
        }
        
        await cache.SetAsync($"announcement:{context.From}", context.SmsBody);
        var body = await localizer.GetLocalizedStringAsync("AnnouncementConfirm", client.Locale, new Dictionary<string, string>() {
                { "smsBody", context.SmsBody }
            });
        var message = new MessagingResponse().Message(body);
        return Monads.Result.Success<MessagingResponse, Exception>(message);
    }
}

internal sealed partial class AnnouncementConfirmCommand(INudgesClient nudgesClient,
                                                         ICacheClient<string> cache,
                                                         ILogger<AnnouncementConfirmCommand> logger,
                                                         KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                         ILocalizationClient localizer) : ITwilioEventCommand {

    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@"^CONFIRM$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();

    public async Task<Monads.Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) {
        var result = await nudgesClient.GetClientByPhoneNumber(context.From, cancellationToken);
        
        if (result.IsError) {
            return new GraphQLException(result.FirstError.Description);
        }

        var client = result.Value;
        if (client.Subscription?.Status != "ACTIVE") {
            return new NotCustomerException();
        }
        
        var announcement = await cache.GetAsync($"announcement:{context.From}");

        if (announcement is null) {
            var errBody = await localizer.GetLocalizedStringAsync("NoPendingAnnouncement", client.Locale);
            return Monads.Result.Success<MessagingResponse, Exception>(new MessagingResponse().Message(errBody));
        }

        var sendResult = await SendMessages(context.From, new Dictionary<string, string> {
                    { "body", announcement.ToString() }
                }, cancellationToken);

        return sendResult.Match(async sent => {
            await cache.RemoveAsync($"announcement:{context.From}");
            var body = await localizer.GetLocalizedStringAsync("AnnouncementSent", client.Locale, new Dictionary<string, string> {
                { "count", sent.ToString(CultureInfo.CurrentCulture) }
            });
            var message = new MessagingResponse().Message(body);
            return Monads.Result.Success<MessagingResponse, Exception>(message);
        }, error => Monads.Result.Failure<MessagingResponse, Exception>(error)).Result;
    }

    private Task<Monads.Result<int, Exception>> SendMessages(string clientPhone, Dictionary<string, string> replacements, CancellationToken cancellationToken) =>
        Task.FromResult(Monads.Result.Success<int, Exception>(0));
}
