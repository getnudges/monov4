using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Auth;
using Nudges.Data.Users;

namespace Nudges.AuthInit;

internal sealed class AuthInitService(ILogger<AuthInitService> logger,
                                      IDbContextFactory<UserDbContext> userDbContextFactory,
                                      IOidcClient oidcClient,
                                      IHostApplicationLifetime appLifetime) : IHostedService {

    private const string DefaultClientPhone = "+15555555555";

    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreDefaultAdmin(cancellationToken);

        appLifetime.StopApplication();
    }

    private async Task StoreDefaultAdmin(CancellationToken cancellationToken) {
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var defaultClient = context.Clients.Where(c => c.PhoneNumber == DefaultClientPhone).FirstOrDefault();
        if (defaultClient is null) {
            logger.LogDefaultClientNotFound(DefaultClientPhone);
            return;
        }

        if (defaultClient.Subject is not null) {
            logger.LogSkippingAdmin(defaultClient.Subject);
            return;
        }

        var userList = await oidcClient.GetUserByUsername(DefaultClientPhone, cancellationToken);

        var adminFound = userList.Match<bool>(users =>
            users.Find(u => u.Username == DefaultClientPhone) is not null,
            e => false);

        if (adminFound) {
            return;
        }

        logger.LogDefaultAdminNotFound(DefaultClientPhone);
        var created = await CreateDefaultAdminInOidc(cancellationToken);

        _ = await created.Match(
            e => {
                logger.LogException(e);
                return Task.CompletedTask;
            },
            async () => {
                var existing = await oidcClient.GetUserByUsername(DefaultClientPhone, cancellationToken);
                var found = existing.Map(users => {
                    return users.Find(u => u.Username == DefaultClientPhone);
                }).Map(user => {
                    defaultClient.Subject = user.Id;
                    context.Clients.Update(defaultClient);
                    context.SaveChanges();
                    return true;
                });
                return found.Match(
                    _ => {
                        logger.LogStoredDefaultAdmin(defaultClient.Id);
                        return Task.CompletedTask;
                    },
                    e => {
                        logger.LogException(e);
                        return Task.CompletedTask;
                    });
            });

    }

    private async Task<Maybe<OidcException>> CreateDefaultAdminInOidc(CancellationToken cancellationToken) =>
        await oidcClient.CreateUser(new UserRepresentation {
            Username = DefaultClientPhone,
            Credentials = [
                    new() {
                        Type = "password",
                        Value = "pass",
                        Temporary = false
                    }
                ],
            Enabled = true,
            // TODO: this is gross
            Groups = [$"admins"],
            RequiredActions = [],
            Attributes = new Dictionary<string, ICollection<string>> {
                    { WellKnownClaims.PhoneNumber, [DefaultClientPhone] },
                    { "phone", [DefaultClientPhone] },
                    { "locale", ["en-US"] }
                }
        }, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) {
        logger.LogServiceStopping();
        return Task.CompletedTask;
    }
}

internal static partial class AuthInitServiceLogs {

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthInitService is stopping.")]
    public static partial void LogServiceStopping(this ILogger<AuthInitService> logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "AuthInitService is starting.")]
    public static partial void LogServiceStarting(this ILogger<AuthInitService> logger);

    [LoggerMessage(Level = LogLevel.Error)]
    public static partial void LogException(this ILogger<AuthInitService> logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin with ID '{AdminId}' has been stored.")]
    public static partial void LogStoredDefaultAdmin(this ILogger<AuthInitService> logger, Guid adminId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin with ID '{AdminId}' already exists.")]
    public static partial void LogSkippingAdmin(this ILogger<AuthInitService> logger, string adminId);
    [LoggerMessage(Level = LogLevel.Warning, Message = "Default client with phone number '{PhoneNumber}' already setup.")]
    public static partial void LogDefaultClientNotFound(this ILogger<AuthInitService> logger, string phoneNumber);
    // log admin user not found in oidc
    [LoggerMessage(Level = LogLevel.Warning, Message = "Default admin user with phone number '{PhoneNumber}' not found in OIDC.")]
    public static partial void LogDefaultAdminNotFound(this ILogger<AuthInitService> logger, string phoneNumber);
}
