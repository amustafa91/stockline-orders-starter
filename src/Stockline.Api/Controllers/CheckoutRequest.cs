public sealed record CheckoutRequest(string? Sku, int Quantity)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Sku)
        && Quantity is >= 1 and <= 100;
}
