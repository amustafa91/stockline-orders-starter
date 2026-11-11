public sealed class CableCampaign : IDiscountPolicy
{
    public DiscountOffer? GetOffer(Product product, int quantity) =>
        product.Sku == "CAB-105" && quantity >= 5 ? new("cable-campaign", 0.15m) : null;
}
