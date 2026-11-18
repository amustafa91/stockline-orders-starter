public sealed class PlaceOrder(
    ProductCatalog catalog, OrderPricing pricing,
    OrderStore store, IPaymentGateway payments,
    LocalOrderConfirmedPublisher confirmed,
    CustomerCatalog customers, CustomerDiscountPolicyFactory customerPolicies)
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

        var customer = customers.Find(command.CustomerId ?? CustomerCatalog.DefaultCustomerId);
        if (customer is null)
            return new OrderRejected("Unknown customer ID.");
        if (!customer.IsActive)
            return new OrderRejected("Inactive customer.");

        var customerPolicy = customerPolicies.Select(customer.Category);
        var total = pricing.Calculate(product, command.Quantity, customerPolicy);
        var order = store.Start(product, command.Quantity, total);
        var decision = await payments.AuthorizeAsync(
            order.OperationKey, order.Total, cancellationToken);
        var completed = store.Complete(order, decision);
        if (completed.Status == "Confirmed")
        {
            var notification = new OrderConfirmed(completed.Id, completed.Sku, completed.Quantity, completed.Total, completed.Currency);
            confirmed.Publish(notification);
        }
        return new OrderPlaced(completed);
    }
}
