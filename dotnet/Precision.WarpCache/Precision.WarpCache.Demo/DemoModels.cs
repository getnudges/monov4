using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Precision.WarpCache.Demo;

public class Customer {
    public int Id { get; set; }
    public string FullName { get; set; }
    public string Email { get; set; }
    public Address Address { get; set; }
    public List<Order> Orders { get; set; }
}
[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Customer))]
public sealed partial class CustomerSerializerContext : JsonSerializerContext { }
public record struct CustomerMessage(Customer Message);

[JsonSourceGenerationOptions(
    GenerationMode = JsonSourceGenerationMode.Default,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(CustomerMessage))]
public sealed partial class CustomerMessageSerializerContext : JsonSerializerContext { }


public class Address {
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string Country { get; set; }
}

public class Order {
    public Guid OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; }
    public PaymentInfo Payment { get; set; }
    public ShippingInfo Shipping { get; set; }
}

public class OrderItem {
    public int Quantity { get; set; }
    public Product Product { get; set; }
    public PriceDetails Price { get; set; }
}

public class Product {
    public int ProductId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public Supplier Supplier { get; set; }
    public List<Review> Reviews { get; set; }
}

public class Supplier {
    public int SupplierId { get; set; }
    public string CompanyName { get; set; }
    public string ContactEmail { get; set; }
}

public class Review {
    public int Rating { get; set; }
    public string Comment { get; set; }
    public string Reviewer { get; set; }
}

public class PriceDetails {
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice => UnitPrice * (1 - Discount);
}

public class PaymentInfo {
    public string PaymentMethod { get; set; }
    public bool Successful { get; set; }
}

public class ShippingInfo {
    public string Carrier { get; set; }
    public string TrackingNumber { get; set; }
    public DateTime EstimatedDelivery { get; set; }
}

