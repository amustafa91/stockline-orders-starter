public sealed record CheckoutRequest(int Quantity)
{
    public bool IsValid => Quantity is >= 1 and <= 100;
}
