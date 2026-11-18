public enum CustomerCategory
{
    Standard,
    Trade
}

public sealed record Customer(string CustomerId, CustomerCategory Category, bool IsActive);

public sealed class CustomerCatalog
{
    public const string DefaultCustomerId = "standard";

    // Local teaching fixtures. Production identity comes from authenticated server context.
    private readonly Customer[] _customers =
    [
        new(DefaultCustomerId, CustomerCategory.Standard, true),
        new("trade", CustomerCategory.Trade, true),
        new("inactive", CustomerCategory.Trade, false)
    ];

    public Customer? Find(string customerId) =>
        _customers.FirstOrDefault(customer => customer.CustomerId == customerId);
}
