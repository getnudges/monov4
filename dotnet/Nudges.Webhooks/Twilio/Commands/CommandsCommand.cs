using System.Text.RegularExpressions;
using Nudges.Localization.Client;
using Nudges.Webhooks.GraphQL;
using Nudges.Webhooks.Twilio;
using Twilio.TwiML;

namespace Nudges.Webhooks.Endpoints.Handlers;

internal sealed partial class CommandsCommand(INudgesClient nudgesClient, ILocalizationClient localizer) : ITwilioEventCommand {
    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@"^commands$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();
    public static readonly Regex HelpRegex = HelpRegexMatcher();
    [GeneratedRegex(@"^help$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex HelpRegexMatcher();

    public async Task<Monads.Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) {
        var result = await nudgesClient.SmsLocaleLookup(context.From, cancellationToken);

        if (result.IsError) {
            return new GraphQLException(result.FirstError.Description);
        }

        var locale = result.Value;
        var body = await localizer.GetLocalizedStringAsync("ClientHelpMessage", locale, cancellationToken);
        var message = new MessagingResponse().Message(body);
        return Monads.Result.Success<MessagingResponse, Exception>(message);
    }
}
