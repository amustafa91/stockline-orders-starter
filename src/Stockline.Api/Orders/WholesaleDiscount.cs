public sealed class WholesaleDiscount : IDiscountPolicy
{
    public DiscountOffer? GetOffer(Product product, int quantity) =>
        quantity >= 10 ? new("wholesale", 0.10m) : null;
}
