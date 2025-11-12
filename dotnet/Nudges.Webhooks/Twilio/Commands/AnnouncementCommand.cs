using System.Globalization;
using System.Text.RegularExpressions;
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

    public async Task<Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) =>
        await nudgesClient.GetClientByPhoneNumber(context.From, cancellationToken).Map<IGetClientByPhoneNumber_ClientByPhoneNumber, MessagingResponse, Exception>(async client => {
            if (client.Subscription?.Status != "ACTIVE") {
                return new NotCustomerException();
            }
            await cache.SetAsync($"announcement:{context.From}", context.SmsBody);
            var body = await localizer.GetLocalizedStringAsync("AnnouncementConfirm", client.Locale, new Dictionary<string, string>() {
                    { "smsBody", context.SmsBody }
                });
            // TODO: KafkaConsumer should handle this
            var message = new MessagingResponse().Message(body);
            return message;
        });
}

internal sealed partial class AnnouncementConfirmCommand(INudgesClient nudgesClient,
                                                         ICacheClient<string> cache,
                                                         ILogger<AnnouncementConfirmCommand> logger,
                                                         KafkaMessageProducer<NotificationKey, NotificationEvent> notificationProducer,
                                                         ILocalizationClient localizer) : ITwilioEventCommand {

    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@"^CONFIRM$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();

    public async Task<Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) =>
        await nudgesClient.GetClientByPhoneNumber(context.From, cancellationToken).Map(async client => {
            if (client.Subscription?.Status != "ACTIVE") {
                return new NotCustomerException();
            }
            var announcement = await cache.GetAsync($"announcement:{context.From}");

            if (announcement is null) {
                var errBody = await localizer.GetLocalizedStringAsync("NoPendingAnnouncement", client.Locale);
                return new MessagingResponse().Message(errBody);
            }

            return await SendMessages(context.From, new Dictionary<string, string> {
                        { "body", announcement.ToString() }
                    }, cancellationToken).Map<int, MessagingResponse, Exception>(async sent => {
                        await cache.RemoveAsync($"announcement:{context.From}");
                        var body = await localizer.GetLocalizedStringAsync("AnnouncementSent", client.Locale, new Dictionary<string, string> {
                            { "count", sent.ToString(CultureInfo.CurrentCulture) }
                        });
                        var message = new MessagingResponse().Message(body);
                        return message;
                    });
        });

    private async Task<Result<int, Exception>> SendMessages(string clientPhone, Dictionary<string, string> replacements, CancellationToken cancellationToken) =>
        await nudgesClient.GetClientByPhoneNumber(clientPhone, cancellationToken).Map<IGetClientByPhoneNumber_ClientByPhoneNumber, int, Exception>(async client => {
            var subs = client.Subscribers?.Nodes?.Select(n => new {
                n.FullPhone,
                n.Locale,
            })?.Where(n => !string.IsNullOrEmpty(n.FullPhone)) ?? [];
            var totalSent = 0;
            foreach (var sub in subs) {
                try {
                    await notificationProducer.Produce(
                        NotificationKey.SendSms(sub.FullPhone!),
                        NotificationEvent.SendSms("Anything", sub.Locale, replacements),
                        cancellationToken);
                    totalSent++;
                } catch (Exception e) {
                    logger.LogSendFailure(e);
                }
            }
            return totalSent;
        });
}
