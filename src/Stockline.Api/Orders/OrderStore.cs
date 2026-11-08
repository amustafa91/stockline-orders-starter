using System.Collections.Concurrent;

public sealed class OrderStore
{
    // Teaching shortcut: restarting the app clears these orders. Persistence comes later.
    private int _sequence = 1047;
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Order Start(Product product, int quantity, decimal total)
    {
        var id = $"ORD-{Interlocked.Increment(ref _sequence)}";
        // Capture the agreed values; later catalog changes must not rewrite this order.
        var order = new Order(id, $"authorize:{id}", product.Sku, quantity,
            total, product.Currency, "Pending");
        _orders[id] = order;
        return order;
    }

    public Order Complete(Order order, PaymentDecision decision)
    {
        var updated = order with
        {
            Status = decision switch
            {
                PaymentDecision.Approved => "Confirmed",
                PaymentDecision.Declined => "Declined",
                _ => "PendingReconciliation"
            }
        };
        _orders[order.Id] = updated;
        return updated;
    }

    public Order? Find(string id) => _orders.GetValueOrDefault(id);
}
