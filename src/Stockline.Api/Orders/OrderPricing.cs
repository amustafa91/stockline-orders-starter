public sealed class OrderPricing
{
    public decimal Calculate(Product product, int quantity)
    {
        var subtotal = product.UnitPrice * quantity;
        var bulkRate = quantity >= 10 ? 0.10m : 0m;
        var campaignRate = product.Sku == "CAB-105" && quantity >= 5 ? 0.15m : 0m;
        // Offers do not stack. Wholesale wins an equal-rate tie.
        var discountRate = campaignRate > bulkRate ? campaignRate : bulkRate;
        return decimal.Round(subtotal * (1m - discountRate), 2, MidpointRounding.AwayFromZero);
    }
}
