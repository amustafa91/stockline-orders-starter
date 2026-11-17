public sealed record ReceiptPreview(string OrderId, decimal Total, string Currency);
public sealed record FulfillmentPreview(string OrderId, string Sku, int Quantity);
public sealed record OrderPreviewState(string OrderId, ReceiptPreview? Receipt,
    FulfillmentPreview? Fulfillment, string[] FailedReactions);

// Local previews only: no email, shipment, durable work or authoritative order state.
public sealed class LocalOrderPreviewStore
{
    public const int Capacity = 100;
    private readonly object _gate = new();
    private readonly Dictionary<string, OrderPreviewState> _states = new();
    private readonly Queue<string> _order = new();

    public void SaveReceipt(OrderConfirmed order) => Update(order.OrderId, state => state with
    { Receipt = new(order.OrderId, order.Total, order.Currency) });

    public void SaveFulfillment(OrderConfirmed order) => Update(order.OrderId, state => state with
    { Fulfillment = new(order.OrderId, order.Sku, order.Quantity) });

    public void RecordFailure(string orderId, string reaction) => Update(orderId, state => state with
    {
        // Failure type names only, bounded even if more reactions are later added.
        FailedReactions = state.FailedReactions.Append(reaction[..Math.Min(reaction.Length, 128)])
            .Distinct().Take(8).ToArray()
    });

    public OrderPreviewState? Find(string orderId)
    {
        lock (_gate)
            return _states.TryGetValue(orderId, out var state)
                ? state with { FailedReactions = state.FailedReactions.ToArray() } : null;
    }

    private void Update(string orderId, Func<OrderPreviewState, OrderPreviewState> update)
    {
        lock (_gate)
        {
            if (!_states.TryGetValue(orderId, out var state))
            {
                if (_states.Count == Capacity) _states.Remove(_order.Dequeue());
                _order.Enqueue(orderId);
                state = new(orderId, null, null, []);
            }
            _states[orderId] = update(state);
        }
    }
}
