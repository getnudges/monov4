using System.Text.RegularExpressions;
using Monads;
using Twilio.TwiML;
using Nudges.Localization.Client;
using Nudges.Webhooks.GraphQL;
using Nudges.Webhooks.Twilio;

namespace Nudges.Webhooks.Endpoints.Handlers;

internal sealed partial class CommandsCommand(INudgesClient nudgesClient, ILocalizationClient localizer) : ITwilioEventCommand {
    public static readonly Regex Regex = RegexMatcher();
    [GeneratedRegex(@"^commands$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex RegexMatcher();
    public static readonly Regex HelpRegex = HelpRegexMatcher();
    [GeneratedRegex(@"^help$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex HelpRegexMatcher();

    public async Task<Result<MessagingResponse, Exception>> InvokeAsync(TwilioEventContext context, CancellationToken cancellationToken) =>
        await nudgesClient.SmsLocaleLookup(context.From, cancellationToken).Map<string, MessagingResponse, Exception>(async locale => {
            var body = await localizer.GetLocalizedStringAsync("ClientHelpMessage", locale, cancellationToken);
            var message = new MessagingResponse().Message(body);
            return message;
        });
}
