public sealed class LocalCheckoutDispatcher
{
    private readonly IPlaceOrderHandler _handler;

    public LocalCheckoutDispatcher(IEnumerable<IPlaceOrderHandler> handlers)
    {
        var registered = handlers.ToArray();
        if (registered.Length != 1)
            throw new InvalidOperationException($"Expected exactly one place-order handler; found {registered.Length}.");
        _handler = registered[0];
    }

    public Task<PlaceOrderResult> Send(PlaceOrderCommand command, CancellationToken cancellationToken) =>
        _handler.Execute(command, cancellationToken);
}
