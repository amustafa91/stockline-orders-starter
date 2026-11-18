public sealed record PlaceOrderCommand(string? Sku, int Quantity, string? CustomerId = null);
