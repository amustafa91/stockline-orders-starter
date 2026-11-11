public sealed record Order(string Id, string OperationKey, string Sku, int Quantity,
    decimal Total, string Currency, string Status);
