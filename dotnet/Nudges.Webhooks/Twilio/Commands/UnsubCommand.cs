using System.Text.RegularExpressions;
using ErrorOr;
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

    public async Task<Monads.Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) {
        if (!int.TryParse(context.Match.Groups[1].Value, out var num)) {
            return new MessageException("Invalid selection");
        }

        var result = await nudgesClient.SmsLocaleLookup(context.From, cancellationToken);
        
        if (result.IsError) {
            return new GraphQLException(result.FirstError.Description);
        }

        var locale = result.Value;
        var body = await localizer.GetLocalizedStringAsync("Unsubscribe", locale, cancellationToken);
        var message = new MessagingResponse().Message(body);
        return Monads.Result.Success<MessagingResponse, Exception>(message);
    }
}
