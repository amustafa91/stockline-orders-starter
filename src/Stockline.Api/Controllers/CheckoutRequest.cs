public sealed record CheckoutRequest(string? Sku, int Quantity, string? CustomerId = null);
