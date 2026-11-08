public sealed class OrderPricing
{
    public decimal Calculate(decimal unitPrice, int quantity)
    {
        var subtotal = unitPrice * quantity;
        var discountRate = quantity >= 10 ? 0.10m : 0m;
        var total = subtotal * (1m - discountRate);
        return decimal.Round(total, 2, MidpointRounding.AwayFromZero);
    }
}
