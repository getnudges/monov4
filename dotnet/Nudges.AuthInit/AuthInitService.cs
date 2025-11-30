using ErrorOr;
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


    public async Task StartAsync(CancellationToken cancellationToken) {
        logger.LogServiceStarting();

        await StoreDefaultAdmin(cancellationToken);

        appLifetime.StopApplication();
    }


    private async Task<User> GetOrCreateDefaultUser(string phoneNumber, UserDbContext dbContext, CancellationToken cancellationToken) {
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(phoneNumber));
        var defaultUser = await dbContext.Users.Include(c => c.Admin)
            .SingleOrDefaultAsync(c => c.PhoneNumberHash == phoneNumberHash, cancellationToken);
        if (defaultUser is { } user) {
            return user;
        }
        logger.LogDefaultUserNotFound(phoneNumber);
        var newUser = new User {
            Locale = "en-US",
            PhoneNumber = phoneNumber,
        };
        var record = dbContext.Users.Add(newUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        return record.Entity;
    }

    private async Task StoreDefaultAdmin(CancellationToken cancellationToken) {
        const string adminPhone = "+15555555555";
        await using var context = await userDbContextFactory.CreateDbContextAsync(cancellationToken);
        var phoneNumberHash = hashService.ComputeHash(ValidationHelpers.NormalizePhoneNumber(adminPhone));
        var defaultUser = await GetOrCreateDefaultUser(adminPhone, context, cancellationToken);

        if (defaultUser.Admin is null) {
            logger.LogDefaultAdminNotFound(adminPhone);
            var adminRecord = context.Admins.Add(new Admin {
                Id = defaultUser.Id
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        var existingAdminCreated = await oidcClient.GetUserByUsername(defaultUser.PhoneNumber, cancellationToken).Then(users =>
            users.Find(u => u.Username == phoneNumberHash)
        ).Then(u => u is not null);

        if (existingAdminCreated.Value) {
            logger.LogSkippingAdmin(defaultUser.Id);
            return;
        }

        var created = await CreateDefaultAdminInOidc(defaultUser.PhoneNumber, phoneNumberHash, cancellationToken).ThenAsync(async _ =>
            await oidcClient.GetUserByUsername(defaultUser.PhoneNumber, cancellationToken).Then(users =>
                    users.Find(u => u.Username == defaultUser.PhoneNumber)
                ).Then(userRep => {
                    defaultUser!.Subject = userRep!.Id!;
                    context.Users.Update(defaultUser);
                    context.SaveChanges();
                    return true;
                }));
    }

    private async Task<ErrorOr<Success>> CreateDefaultAdminInOidc(string phoneNumber, string phoneNumberHash, CancellationToken cancellationToken) =>
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
