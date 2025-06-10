using Microsoft.EntityFrameworkCore;

namespace ProductApi.Tests.Integration;

[Collection(nameof(BasicTests))]
public sealed class BasicTests(ApiFactory factory) : IAsyncLifetime {
    private readonly ApiFactory _factory = factory;

    private async Task<Client> CreateTestClient() {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        var testClient = await dbContext.Clients.AddAsync(new Client {
            Name = "Test Client",
            PhoneNumber = "+12222222222",
            Locale = "en-US"
        });
        await dbContext.SaveChangesAsync();
        return testClient.Entity;
    }

    [Fact]
    public async Task GetClientReturnsClient() {
        var client = await CreateTestClient();

        await using var result = await _factory.ExecuteRequestAsync(r =>
            r.SetQuery($$"""
              query {
                client(id: "{{client.Id}}") {
                  name
                }
              }
              """));
        result.Errors.Should()
            .BeNullOrEmpty();
        result.Data
            .MatchSnapshot(extension: ".json");
    }

    public async Task InitializeAsync() {
        using var scope = _factory.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<UserDbContext>>();
        await using var dbContext = await factory.CreateDbContextAsync();
        dbContext.Database.SetCommandTimeout(160);
        await dbContext.Database.MigrateAsync();
        var testSubscriber = await dbContext.Subscribers.AddAsync(new Subscriber {
            PhoneNumber = "+11234567890"
        });
        var testClient = await dbContext.Clients.AddAsync(new Client {
            Name = "Test Client",
            PhoneNumber = "+11111111111",
            Locale = "en-US",
            SubscriberPhoneNumbers = [
                new() {
                    PhoneNumber = testSubscriber.Entity.PhoneNumber
                }
            ],
            Announcements = [
                new Announcement {
                   MessageSid = "SM4c09151fa7582245042aa94dffd68a4d",
                   SentOn = DateTime.UtcNow,
                }
            ]
        });
        await dbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _factory.DisposeAsync();
}



