using System.Collections.Concurrent;

public sealed class OrderStore
{
    // Teaching shortcut: restarting the app clears these orders. Persistence comes later.
    private int _sequence = 1047;
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public Order Start(int quantity)
    {
        var id = $"ORD-{Interlocked.Increment(ref _sequence)}";
        // The starter uses a fixed USD 12 unit price so Lesson 1 can focus on provider coupling.
        var order = new Order(id, $"authorize:{id}", "BRG-204", quantity,
            12m * quantity, "USD", "Pending");
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
