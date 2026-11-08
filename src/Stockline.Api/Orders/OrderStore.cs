using System.Collections.Concurrent;

public sealed class OrderStore
{
    // Teaching shortcut: restarting the app clears these orders. Persistence comes later.
    private int _sequence = 1047;
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Order Start(int quantity)
    {
        var id = $"ORD-{Interlocked.Increment(ref _sequence)}";
        // Catalog fixture: 199 cents per unit.
        var unitPriceCents = 199;
        var unitPriceUsd = unitPriceCents / 100m;
        var totalUsd = unitPriceUsd * quantity;
        var order = new Order(id, $"authorize:{id}", "BRG-204", quantity,
            totalUsd, "USD", "Pending");
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
