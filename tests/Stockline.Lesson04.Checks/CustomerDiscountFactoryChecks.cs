using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

internal static class CustomerDiscountFactoryChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var customers = new CustomerCatalog();
        var factory = new CustomerDiscountPolicyFactory();
        var catalog = new ProductCatalog();
        var cable = catalog.Find("CAB-105")!;
        var bearing = catalog.Find("BRG-204")!;
        var standard = factory.Select(CustomerCategory.Standard);
        var trade = factory.Select(CustomerCategory.Trade);
        check(customers.Find("standard") is { Category: CustomerCategory.Standard, IsActive: true },
            "Seeded Standard customer is active");
        check(customers.Find("trade") is { Category: CustomerCategory.Trade, IsActive: true },
            "Seeded Trade customer is active");
        check(customers.Find("inactive") is { IsActive: false } && customers.Find("missing") is null,
            "Inactive and unknown IDs remain distinct");
        check(standard is NoCustomerDiscount && standard.GetOffer(cable, 10) is null,
            "Factory selects the Standard no-offer rule");
        check(trade is TradeCustomerDiscount, "Factory selects the Trade rule by category");
        check(trade.GetOffer(cable, 9) is null, "Trade quantity 9 is ineligible");
        check(trade.GetOffer(cable, 10) is { Code: "trade-customer", Rate: 0.20m }, "Trade quantity 10 is eligible");
        check(trade.GetOffer(cable, 11) is { Rate: 0.20m }, "Trade quantity 11 remains eligible");
        try
        {
            factory.Select((CustomerCategory)99);
            check(false, "Unknown category must throw");
        }
        catch (ArgumentOutOfRangeException error)
        {
            check(error.ParamName == "category", "Unknown category throws with no fallback");
        }

        var pricing = new OrderPricing([new WholesaleDiscount(), new CableCampaign()]);
        check(pricing.SelectOffer(cable, 9, trade) is { Code: "cable-campaign" },
            "The Cable campaign wins when the selected Trade rule returns no offer");
        check(pricing.Calculate(cable, 9, trade) == 57.38m, "Trade CAB 9 is 57.38");
        check(pricing.Calculate(cable, 10, trade) == 60m, "Trade CAB 10 selects only the largest offer");
        check(pricing.Calculate(cable, 11, trade) == 66m, "Trade CAB 11 is 66.00");
        check(pricing.Calculate(bearing, 10, trade) == 96m, "Trade BRG 10 is 96.00");
        check(pricing.Calculate(cable, 10, standard) == 63.75m, "Standard CAB 10 keeps the existing total");
        check(pricing.SelectOffer(bearing, 10, new FixedOfferPolicy("customer-tie", .10m))?.Code == "wholesale",
            "An equal customer offer leaves the earlier base winner in place");

        var store = new OrderStore();
        var payments = new RecordingGateway();
        var previews = new LocalOrderPreviewStore();
        var publisher = new LocalOrderConfirmedPublisher(
            [new ReceiptPreviewReaction(previews), new FulfillmentPreviewReaction(previews)], previews);
        var workflow = new PlaceOrder(catalog, pricing, store, payments, publisher, customers, factory);
        var controller = new OrdersController(new LocalCheckoutDispatcher([workflow]), store);

        foreach (var (customerId, error) in new[] { ("missing", "Unknown customer ID."), ("inactive", "Inactive customer.") })
        {
            var result = await controller.Place(new("CAB-105", 10, customerId), default);
            check(result is BadRequestObjectResult rejected
                && System.Text.Json.JsonSerializer.Serialize(rejected.Value).Contains(error),
                $"{customerId}: explicit HTTP 400");
            check(payments.Calls == 0 && store.Find("ORD-1048") is null,
                $"{customerId}: no authorization and no stored order");
        }

        var count = 0;
        foreach (var (id, total) in new (string? Id, decimal Total)[] { (null, 63.75m), ("standard", 63.75m), ("trade", 60m) })
        {
            var result = await controller.Place(new("CAB-105", 10, id), default);
            var order = (result as CreatedResult)?.Value as Order;
            check(order is not null && order.Total == total && store.Find(order.Id) == order
                && payments.LastAmount == total && payments.Calls == ++count,
                $"{id ?? "omitted"}: stored and authorized total {total}");
        }

        var services = new ServiceCollection();
        services.AddStocklineCheckout();
        check(services.Any(d => d.ServiceType == typeof(CustomerCatalog) && d.Lifetime == ServiceLifetime.Singleton)
           && services.Any(d => d.ServiceType == typeof(CustomerDiscountPolicyFactory) && d.Lifetime == ServiceLifetime.Singleton),
            "Composition explicitly registers the catalog and policy factory");
    }
}
