//using UnAd.Redis.Models;

//namespace UnAd.Webhooks;

//public static class ProductExtensions {
//    public static Product ToProduct(this Stripe.Product stripeProduct) => new() {
//        Id = stripeProduct.Id,
//        Name = stripeProduct.Name,
//        Description = stripeProduct.Description
//    };
//    public static Product ToProduct(this StackExchange.Redis.HashEntry[] hashEntries, string productId) {
//        var dict = hashEntries.ToDictionary(k => k.Name.ToString(), v => v.Value.ToString());
//        return new() {
//            Id = productId,
//            Name = dict["name"],
//            Description = dict["description"]
//        };
//    }
//}
