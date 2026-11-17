// A local notification built from the saved confirmed order, not a durable event.
public sealed record OrderConfirmed(string OrderId, string Sku, int Quantity, decimal Total, string Currency);
