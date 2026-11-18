using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal static class OrderConfirmedReactionChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        foreach (var failReceipt in new[] { false, true })
        {
            var store = new OrderStore();
            var previews = new LocalOrderPreviewStore();
            var gateway = new PreviewGateway();
            var workflow = Workflow(store, gateway, previews,
                new ReceiptPreviewReaction(previews, failReceipt), new FulfillmentPreviewReaction(previews));
            var placed = (OrderPlaced)await workflow.Execute(new("CAB-105", 10), default);
            var state = previews.Find(placed.Order.Id)!;
            check(placed.Order.Status == "Confirmed" && store.Find(placed.Order.Id) == placed.Order
                && placed.Order.Total == 63.75m && gateway.Calls == 1 && gateway.Amount == placed.Order.Total,
                "Saved confirmation and original payment amount survive preview failure");
            check(state.Fulfillment?.Sku == "CAB-105" && state.Fulfillment.Quantity == 10,
                "Fulfillment preview still runs after receipt-preview failure");
            check(failReceipt ? state.Receipt is null && state.FailedReactions.SequenceEqual([nameof(ReceiptPreviewReaction)])
                : state.Receipt?.Total == placed.Order.Total && state.FailedReactions.Length == 0,
                "Receipt success or isolated failure is visible without changing authoritative state");
        }

        foreach (var decision in new[] { PaymentDecision.Declined, PaymentDecision.Unknown })
        {
            var store = new OrderStore();
            var previews = new LocalOrderPreviewStore();
            var calls = 0;
            var subscriber = new ProbeReaction(_ => calls++);
            var gateway = new PreviewGateway((_, _, _) => Task.FromResult(decision));
            var placed = (OrderPlaced)await Workflow(store, gateway, previews, subscriber)
                .Execute(new("CAB-105", 10), default);
            check(calls == 0 && previews.Find(placed.Order.Id) is null && gateway.Calls == 1
                && placed.Order.Status == (decision == PaymentDecision.Declined ? "Declined" : "PendingReconciliation"),
                $"{decision} saves its original state and invokes no confirmed-order subscriber");
        }

        var orderedStore = new OrderStore();
        var orderedPreviews = new LocalOrderPreviewStore();
        var invocationOrder = new List<string>();
        var payloads = new List<OrderConfirmed>();
        var receipt = new ProbeReaction(order =>
        {
            if (orderedStore.Find(order.OrderId)?.Status != "Confirmed")
                throw new Exception("Notification arrived before confirmation was saved");
            invocationOrder.Add("receipt"); payloads.Add(order); orderedPreviews.SaveReceipt(order);
        });
        var fulfillment = new ProbeReaction(order =>
        {
            if (orderedPreviews.Find(order.OrderId)?.Receipt is null)
                throw new Exception("Not sequential: receipt has not completed");
            invocationOrder.Add("fulfillment"); payloads.Add(order); orderedPreviews.SaveFulfillment(order);
        });
        var orderedGateway = new PreviewGateway();
        var ordered = (OrderPlaced)await Workflow(orderedStore, orderedGateway, orderedPreviews, receipt, fulfillment)
            .Execute(new("CAB-105", 10), default);
        check(invocationOrder.SequenceEqual(["receipt", "fulfillment"]) && receipt.Calls == 1 && fulfillment.Calls == 1,
            "Successful invocation calls each subscriber exactly once, sequentially after saved confirmation");
        check(payloads.Count == 2 && ReferenceEquals(payloads[0], payloads[1])
            && payloads[0] == new OrderConfirmed(ordered.Order.Id, "CAB-105", 10, 63.75m, "USD"),
            "Both subscribers receive the same immutable data from the saved order");

        foreach (var failurePosition in new[] { 0, 1 })
        {
            var store = new OrderStore();
            var previews = new LocalOrderPreviewStore();
            var first = new ProbeReaction(order =>
            {
                if (failurePosition == 0) throw new InvalidOperationException("first callback failed");
                previews.SaveReceipt(order);
            });
            var second = new ProbeReaction(order =>
            {
                if (failurePosition == 1) throw new InvalidOperationException("second callback failed");
                previews.SaveFulfillment(order);
            });
            var placed = (OrderPlaced)await Workflow(store, new PreviewGateway(), previews, first, second)
                .Execute(new("CAB-105", 10), default);
            check(first.Calls == 1 && second.Calls == 1 && store.Find(placed.Order.Id)?.Status == "Confirmed",
                $"Subscriber failure at position {failurePosition} neither skips another subscriber nor reverses confirmation");
            var state = previews.Find(placed.Order.Id)!;
            check(state.FailedReactions.SequenceEqual([nameof(ProbeReaction)])
                && (failurePosition == 0 ? state.Receipt is null && state.Fulfillment is not null
                    : state.Receipt is not null && state.Fulfillment is null),
                "Partial preview success and bounded failure type remain inspectable");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var previews = new LocalOrderPreviewStore();
            var later = new ProbeReaction(previews.SaveFulfillment);
            var fails = new ProbeReaction(_ => throw new OperationCanceledException(cancellation.Token));
            var store = new OrderStore();
            var placed = (OrderPlaced)await Workflow(store, new PreviewGateway(), previews, fails, later)
                .Execute(new("CAB-105", 10), default);
            check(placed.Order.Status == "Confirmed" && later.Calls == 1 && previews.Find(placed.Order.Id)?.Fulfillment is not null,
                "A subscriber cancellation exception is isolated as preview failure, not caller cancellation");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            foreach (var error in new Exception[] { new InvalidOperationException("gateway failure"), new OperationCanceledException(cancellation.Token) })
            {
                var subscriber = new ProbeReaction(_ => throw new Exception("must not run"));
                var store = new OrderStore();
                var previews = new LocalOrderPreviewStore();
                var gateway = new PreviewGateway((_, _, _) => Task.FromException<PaymentDecision>(error));
                Exception? observed = null;
                try { await Workflow(store, gateway, previews, subscriber).Execute(new("CAB-105", 10), cancellation.Token); }
                catch (Exception exception) { observed = exception; }
                check(ReferenceEquals(observed, error) && gateway.Calls == 1 && gateway.Token == cancellation.Token
                    && subscriber.Calls == 0 && previews.Find("ORD-1048") is null
                    && store.Find("ORD-1048")?.Status == "Pending",
                    $"Original {error.GetType().Name} propagates before confirmation with no reactions");
            }
        }
        using (var cancellation = new CancellationTokenSource())
        {
            var previews = new LocalOrderPreviewStore();
            var reaction = new ProbeReaction(previews.SaveReceipt);
            var gateway = new PreviewGateway((_, _, _) => { cancellation.Cancel(); return Task.FromResult(PaymentDecision.Approved); });
            var placed = (OrderPlaced)await Workflow(new OrderStore(), gateway, previews, reaction)
                .Execute(new("CAB-105", 10), cancellation.Token);
            check(placed.Order.Status == "Confirmed" && reaction.Calls == 1 && previews.Find(placed.Order.Id)?.Receipt?.Total == 63.75m,
                "Cancellation racing with an inner approval does not add a new cancellation check or undo reactions");
        }

        var invalidReaction = new ProbeReaction(_ => throw new Exception("must not run"));
        var invalidGateway = new PreviewGateway();
        var invalid = Workflow(new OrderStore(), invalidGateway, new LocalOrderPreviewStore(), invalidReaction);
        foreach (var command in new[] { new PlaceOrderCommand("UNKNOWN", 10), new("CAB-105", 0) })
            check(await invalid.Execute(command, default) is OrderRejected && invalidGateway.Calls == 0 && invalidReaction.Calls == 0,
                "Rejected input invokes neither payment nor confirmed-order reactions");

        var repeatedPreviews = new LocalOrderPreviewStore();
        var repeated = new ProbeReaction(_ => { });
        var publisher = new LocalOrderConfirmedPublisher([repeated], repeatedPreviews);
        var notification = new OrderConfirmed("fixture", "CAB-105", 10, 63.75m, "USD");
        publisher.Publish(notification); publisher.Publish(notification);
        check(repeated.Calls == 2, "Publishing twice invokes twice: local callbacks promise no deduplication or exactly-once delivery");
        new LocalOrderConfirmedPublisher([], repeatedPreviews).Publish(notification);
        check(repeatedPreviews.Find("fixture") is null, "No subscriber is valid and creates no durable publication record");
        var restarted = new LocalOrderPreviewStore();
        check(restarted.Find(ordered.Order.Id) is null, "A new process-local preview store loses previous previews");

        var bounded = new LocalOrderPreviewStore();
        for (var index = 0; index <= LocalOrderPreviewStore.Capacity; index++)
            bounded.SaveReceipt(notification with { OrderId = "order-" + index });
        check(bounded.Find("order-0") is null && bounded.Find("order-100")?.Receipt?.Total == 63.75m,
            "Preview retention is bounded to 100 orders and evicts the oldest first-observed order");
        bounded.RecordFailure("order-100", "probe");
        var detached = bounded.Find("order-100")!;
        detached.FailedReactions[0] = "mutated";
        check(bounded.Find("order-100")!.FailedReactions[0] == "probe", "Preview read arrays are detached from retained state");
        Parallel.For(0, 1000, index =>
        {
            if (index % 2 == 0) bounded.SaveReceipt(notification);
            else bounded.SaveFulfillment(notification);
        });
        check(bounded.Find("fixture") is { Receipt: not null, Fulfillment: not null },
            "Concurrent preview updates preserve both projections for the retained order");
        foreach (var environment in new[] { "Development", "Production" })
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["OrderPreviews:FailReceipt"] = "true" }).Build());
            services.AddSingleton<IHostEnvironment>(new CheckHostEnvironment { EnvironmentName = environment });
            services.AddStocklineCheckout();
            await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
                { ValidateScopes = true, ValidateOnBuild = true });
            await using var scope = provider.CreateAsyncScope();
            var placed = (OrderPlaced)await scope.ServiceProvider.GetRequiredService<PlaceOrder>()
                .Execute(new("CAB-105", 10), default);
            var preview = provider.GetRequiredService<LocalOrderPreviewStore>().Find(placed.Order.Id)!;
            check(placed.Order.Status == "Confirmed" && preview.Fulfillment is not null
                && (environment == "Development" ? preview.Receipt is null && preview.FailedReactions.Length == 1
                    : preview.Receipt?.Total == 63.75m && preview.FailedReactions.Length == 0),
                $"Receipt failure fixture is Development-only: {environment} keeps the saved order and configured behavior");
        }
        var development = new OrderPreviewsController(bounded, new TestEnvironment("Development"));
        check(development.Get("fixture") is OkObjectResult && development.Get("missing") is NotFoundResult,
            "Development preview reads expose retained state and return 404 when absent");
        check(new OrderPreviewsController(bounded, new TestEnvironment("Production")).Get("fixture") is NotFoundResult,
            "Preview reads are hidden outside Development");
    }

    private static PlaceOrder Workflow(OrderStore store, IPaymentGateway gateway, LocalOrderPreviewStore previews,
        params IOrderConfirmedReaction[] reactions) => new(new ProductCatalog(),
            new OrderPricing([new WholesaleDiscount(), new CableCampaign()]), store, gateway,
            new LocalOrderConfirmedPublisher(reactions, previews),
            new CustomerCatalog(), new CustomerDiscountPolicyFactory());

    private sealed class ProbeReaction(Action<OrderConfirmed> invoke) : IOrderConfirmedReaction
    {
        public int Calls;
        public void OnConfirmed(OrderConfirmed order) { Calls++; invoke(order); }
    }
    private sealed class PreviewGateway(Func<string, decimal, CancellationToken, Task<PaymentDecision>>? invoke = null) : IPaymentGateway
    {
        public int Calls;
        public decimal Amount;
        public CancellationToken Token;
        public Task<PaymentDecision> AuthorizeAsync(string key, decimal amount, CancellationToken token)
        {
            Calls++; Amount = amount; Token = token;
            if (invoke is not null) return invoke(key, amount, token);
            token.ThrowIfCancellationRequested(); return Task.FromResult(PaymentDecision.Approved);
        }
    }
    private sealed class TestEnvironment(string name) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Stockline.Checks";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
