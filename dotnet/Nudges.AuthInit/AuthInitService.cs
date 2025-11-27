using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Monads;
using Nudges.Auth;
using Nudges.Data.Users;
using Nudges.Data.Users.Models;
using Nudges.Security;

namespace Nudges.AuthInit;

internal sealed class AuthInitService(ILogger<AuthInitService> logger,
                                      IDbContextFactory<UserDbContext> userDbContextFactory,
                                      IOidcClient oidcClient,
                                      HashService hashService,
                                      IHostApplicationLifetime appLifetime) : IHostedService {

    private const string AdminPhone = "+15555555555";

    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreDefaultAdmin(cancellationToken);

        appLifetime.StopApplication();
    }


    private async Task<User> GetOrCreateDefaultUser(UserDbContext dbContext, string phoneNumberHash, CancellationToken cancellationToken) {
        var defaultUser = await dbContext.Users.Include(c => c.Admin)
            .SingleOrDefaultAsync(c => c.PhoneNumberHash == phoneNumberHash, cancellationToken);
        if (defaultUser is { } user) {
            return user;
        }
        logger.LogDefaultUserNotFound(AdminPhone);
        var newUser = new User {
            Locale = "en-US",
            PhoneNumber = AdminPhone,
        };
        var record = dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        return record.Entity;
    }

    private async Task StoreDefaultAdmin(CancellationToken cancellationToken) {
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(AdminPhone));
        var defaultUser = await GetOrCreateDefaultUser(context, phoneNumberHash, cancellationToken);

        if (defaultUser.Admin is null) {
            logger.LogDefaultAdminNotFound(AdminPhone);
            var adminRecord = context.Admins.Add(new Admin {
                Id = defaultUser.Id
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        var existingAdmin = await oidcClient.GetUserByUsername(AdminPhone, cancellationToken);
        existingAdmin.Map(users =>
            users.Find(u => u.Username == phoneNumberHash)
        );

        var created = await CreateDefaultAdminInOidc(AdminPhone, phoneNumberHash, cancellationToken);

        _ = await created.Match(async () => {
            var newlyCreated = await oidcClient.GetUserByUsername(AdminPhone, cancellationToken);
            var found = newlyCreated.Map(users =>
                users.Find(u => u.Username == phoneNumberHash)
            )
            .Map(userRep => {
                defaultUser!.Subject = userRep!.Id!;
                context.Users.Update(defaultUser);
                context.SaveChanges();
                return true;
            });
            return found.Match(
                _ => {
                    logger.LogStoredDefaultAdmin(defaultUser!.Id);
                    return Task.CompletedTask;
                },
                e => {
                    logger.LogException(e);
                    return Task.CompletedTask;
                });
        },
            async e => {
                logger.LogException(e);
                return Task.CompletedTask;
            });
    }

    private async Task<Maybe<OidcException>> CreateDefaultAdminInOidc(string phoneNumber, string phoneNumberHash, CancellationToken cancellationToken) =>
        await oidcClient.CreateUser(new UserRepresentation {
            /*
             * ***NOTE:***
             * The username is the ONLY place we store the phone number in plain text
             */
            Username = phoneNumber,
            Credentials = [
                    new() {
                        Type = "password",
                        Value = "pass",
                        Temporary = false
                    }
                ],
            Enabled = true,
            Groups = [$"admins"],
            RequiredActions = [],
            Attributes = new Dictionary<string, ICollection<string>> {
                    { WellKnownClaims.PhoneNumber, [phoneNumberHash] },
                    { "phone", [phoneNumberHash] },
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
    public static partial void LogSkippingAdmin(this ILogger<AuthInitService> logger, Guid adminId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default user with phone number '{PhoneNumber}' not found.")]
    public static partial void LogDefaultUserNotFound(this ILogger<AuthInitService> logger, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Information, Message = "Default admin user with phone number '{PhoneNumber}' not found in OIDC.")]
    public static partial void LogDefaultAdminNotFound(this ILogger<AuthInitService> logger, string phoneNumber);
}
