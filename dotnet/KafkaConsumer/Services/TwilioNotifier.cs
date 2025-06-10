using KafkaConsumer.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio.Clients;
using Twilio.Rest.Api.V2010.Account;
using Nudges.Configuration.Extensions;

namespace KafkaConsumer.Notifications;

internal class TwilioNotifier(IConfiguration config, Func<ITwilioRestClient> twilioClientFunc, ILogger<TwilioNotifier> logger) : INotifier {
    private readonly string _messageServiceSid = config.GetTwilioMessageServiceSid();
    public async Task Notify(string phoneNumber, string message, CancellationToken cancellationToken = default) {
        var resource = await MessageResource.CreateAsync(new CreateMessageOptions(phoneNumber) {
            MessagingServiceSid = _messageServiceSid,
            Body = message,
        }, twilioClientFunc());
        logger.LogMessageSent(phoneNumber, resource.Sid);
    }
}
internal static partial class TwilioNotifierLogs {
    [LoggerMessage(Level = LogLevel.Debug, Message = "Sent SMS to {PhoneNumber} with SID {Sid}")]
    public static partial void LogMessageSent(this ILogger<TwilioNotifier> logger, string phoneNumber, string sid);
}

