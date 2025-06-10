using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Monads;
using Twilio.TwiML;

namespace UnAd.Webhooks.Twilio;

public record TwilioEventContext(string SmsBody, string From, string Locale, Match Match);

public sealed class TwilioEventCommandProcessorBuilder() {

    private readonly ImmutableArray<KeyValuePair<Regex, ITwilioEventCommand>> _commands = [];

    private TwilioEventCommandProcessorBuilder(IEnumerable<KeyValuePair<Regex, ITwilioEventCommand>> commands) : this() =>
        _commands = [.. commands];

    public TwilioEventCommandProcessorBuilder AddHandler(Regex checker, ITwilioEventCommand command) =>
        new(_commands.Concat([new KeyValuePair<Regex, ITwilioEventCommand>(checker, command)]));

    public TwilioMessageProcessor Build() => new(_commands);
}

public interface ITwilioEventCommand {
    public Task<Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken);
}

public sealed partial class TwilioMessageProcessor(IEnumerable<KeyValuePair<Regex, ITwilioEventCommand>> commands) {

    public async Task<Result<MessagingResponse, Exception>> ProcessEvent(string smsBody, string from, string locale, CancellationToken cancellationToken) {

        foreach (var command in commands) {
            if (command.Key.Match(smsBody.Trim()) is Match match && match.Length > 0) {
                try {
                    return await command.Value.InvokeAsync(new TwilioEventContext(smsBody, from, locale, match), cancellationToken);
                } catch (Exception e) {
                    return new MessageException("Could not process message", e);
                }
            }
        }
        return new UnhandledMessageException();
    }
}

public class UnhandledMessageException() : Exception("Message Unhandled");

public class MessageException : Exception {
    public MessageException(string message) : base(message) { }
    public MessageException(string message, Exception innerException) : base(message, innerException) { }
}
