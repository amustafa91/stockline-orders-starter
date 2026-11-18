public sealed class CustomerDiscountPolicyFactory
{
    private readonly NoCustomerDiscount _standard = new();
    private readonly TradeCustomerDiscount _trade = new();

    public IDiscountPolicy Select(CustomerCategory category) => category switch
    {
        CustomerCategory.Standard => _standard,
        CustomerCategory.Trade => _trade,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown customer category.")
    };
}

public sealed class NoCustomerDiscount : IDiscountPolicy
{
    public DiscountOffer? GetOffer(Product product, int quantity) => null;
}

public sealed class TradeCustomerDiscount : IDiscountPolicy
{
    public DiscountOffer? GetOffer(Product product, int quantity) =>
        quantity >= 10 ? new("trade-customer", 0.20m) : null;
}
