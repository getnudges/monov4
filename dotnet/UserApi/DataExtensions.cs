using UnAd.Data.Users.Models;

namespace UserApi;

public static class DataExtensions {
    public static IQueryable<Client> WithActiveSubscriptions(this IQueryable<Client> clients) =>
        clients.Where(clients => !string.IsNullOrEmpty(clients.SubscriptionId));
    public static IQueryable<Client> WithNoSubscribers(this IQueryable<Client> clients) =>
        clients.Where(c => c.SubscriberPhoneNumbers.Count() == 0);
}
