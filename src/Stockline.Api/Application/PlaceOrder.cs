public sealed class PlaceOrder(
    ProductCatalog catalog, OrderPricing pricing,
    OrderStore store, IPaymentGateway payments)
{
    public async Task<PlaceOrderResult> Execute(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Sku)
            || command.Quantity is < 1 or > 100)
            return new OrderRejected("Provide a SKU and quantity from 1 to 100.");

        var product = catalog.Find(command.Sku);
        if (product is null)
            return new OrderRejected("Unknown product SKU.");

        var total = pricing.Calculate(product, command.Quantity);
        var order = store.Start(product, command.Quantity, total);
        var decision = await payments.AuthorizeAsync(
            order.OperationKey, order.Total, cancellationToken);
        return new OrderPlaced(store.Complete(order, decision));
    }
}
