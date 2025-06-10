using Twilio.Rest.Api.V2010.Account;
using Nudges.Configuration.Extensions;

namespace Nudges.Webhooks;

public interface IMessageSender {
    public Task Send(string phoneNumber, string message);
}

public class MessageSender(IConfiguration config) : IMessageSender {
    private readonly string _messageServiceSid = config.GetTwilioMessageServiceSid();

    public Task Send(string phoneNumber, string message) =>
        MessageResource.CreateAsync(new CreateMessageOptions(phoneNumber) {
            MessagingServiceSid = _messageServiceSid,
            Body = message,
        });
}
