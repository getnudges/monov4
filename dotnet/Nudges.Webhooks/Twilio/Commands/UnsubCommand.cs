using System.Text.RegularExpressions;
using Monads;
using Twilio.TwiML;
using Nudges.Localization.Client;
using Nudges.Webhooks.GraphQL;
using Nudges.Webhooks.Twilio;

namespace Nudges.Webhooks.Endpoints.Handlers;

internal sealed partial class UnsubCommand(INudgesClient nudgesClient, ILocalizationClient localizer) : ITwilioEventCommand {
    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@"UNSUB (\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();

    public async Task<Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) {
        if (!int.TryParse(context.Match.Groups[1].Value, out var num)) {
            return new MessageException("Invalid selection");
        }

        return await nudgesClient.SmsLocaleLookup(context.From, cancellationToken).Map<string, MessagingResponse, Exception>(async locale => {
            var body = await localizer.GetLocalizedStringAsync("Unsubscribe", locale, cancellationToken);
            var message = new MessagingResponse().Message(body);
            return message;
        });
    }
}
