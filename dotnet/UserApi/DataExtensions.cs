using Nudges.Data.Users.Models;

namespace UserApi;

public static class DataExtensions {
    public static IQueryable<Client> WithActiveSubscriptions(this IQueryable<Client> clients) =>
        clients.Where(clients => !string.IsNullOrEmpty(clients.SubscriptionId));
    public static IQueryable<Client> WithNoSubscribers(this IQueryable<Client> clients) =>
        clients.Where(c => c.Subscribers.Count() == 0);

    public static Nudges.Contracts.Client ToContractClient(this Client client, string nodeId) =>
        new(
            client.Id,
            nodeId,
            client.Name,
            client.Slug,
            client.CustomerId,
            client.SubscriptionId);
}
