using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

var passed = 0;
void Check(bool value, string label)
{
    if (!value) throw new Exception(label);
    Console.WriteLine($"PASS {++passed}: {label}");
}
var pricing = new OrderPricing();
var catalog = new ProductCatalog();
var store = new OrderStore();
var gateway = new RecordingGateway();
var useCase = new PlaceOrder(catalog, pricing, store, gateway);
var controller = new OrdersController(useCase, store);
foreach (var item in new[] { ("BRG-204", 24m), ("CAB-105", 15m) })
{
    var response = await controller.Place(new(item.Item1, 2), default);
    Check(response is CreatedResult, $"{item.Item1} creates an order");
    var order = (Order)((CreatedResult)response).Value!;
    Check(order.Total == item.Item2 && gateway.LastAmount == order.Total,
        $"{item.Item1}: receipt and payment both {item.Item2}");
    Check(store.Find(order.Id) == order && order.Status == "Confirmed", "GET source preserves the order");
}
foreach (var request in new[] { new CheckoutRequest(null, 2), new("", 2), new(" ", 2), new("MISSING", 2), new("BRG-204", 0), new("BRG-204", -1), new("BRG-204", 101) })
{
    var calls = gateway.Calls;
    var response = await controller.Place(request, default);
    Check(response is BadRequestObjectResult && gateway.Calls == calls, $"Rejected before payment: {request}");
}
var next = (Order)((CreatedResult)await controller.Place(new("BRG-204", 1), default)).Value!;
Check(next.Id == "ORD-1050", "Rejected inputs did not consume order identities or create orders");
foreach (var qty in new[] { 1, 100 })
    Check(await controller.Place(new("BRG-204", qty), default) is CreatedResult, $"Quantity {qty} accepted");
var forged = JsonSerializer.Deserialize<CheckoutRequest>("""{"sku":"CAB-105","quantity":2,"unitPrice":0.01,"total":0.02}""", new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
var forgedOrder = (Order)((CreatedResult)await controller.Place(forged, default)).Value!;
Check(forgedOrder.Total == 15m && gateway.LastAmount == 15m, "Extra client prices cannot influence authoritative amount");
var old = store.Start(new("BRG-204", 12m, "USD"), 2, 24m);
var updated = store.Start(new("BRG-204", 14m, "USD"), 2, 28m);
Check(store.Find(old.Id)!.Total == 24m && updated.Total == 28m, "Different product versions do not rewrite the old receipt");
foreach (var (decision, status, responseCode) in new[] { (PaymentDecision.Declined, "Declined", 409), (PaymentDecision.Unknown, "PendingReconciliation", 202) })
{
    gateway.Decision = decision;
    var response = (ObjectResult)await controller.Place(new("CAB-105", 2), default);
    Check(((Order)response.Value!).Status == status && response.StatusCode == responseCode, $"Preserved {decision} mapping");
}
gateway.Decision = PaymentDecision.Approved;
foreach (var (quantity, expected) in new[] { (9, 108m), (10, 108m), (11, 118.80m) })
{
    Check(pricing.Calculate(12m, quantity) == expected, $"Pricing boundary {quantity}: {expected}");
    var priced = (Order)((CreatedResult)await controller.Place(new("BRG-204", quantity), default)).Value!;
    Check(priced.Total == expected && gateway.LastAmount == expected, $"Receipt/payment boundary {quantity}");
}
Check(pricing.Calculate(1.005m, 1) == 1.01m, "Positive midpoint rounds away from zero");
Check(pricing.Calculate(0.335m, 3) == 1.01m, "Round the final total, not each unit");
Check(pricing.Calculate(7.5m, 10) == 67.50m, "Wholesale CAB price");
using var cts = new CancellationTokenSource(); cts.Cancel();
try { await controller.Place(new("BRG-204", 2), cts.Token); throw new Exception("Cancellation ignored"); }
catch (OperationCanceledException) { Check(gateway.LastToken == cts.Token, "Cancellation reaches gateway"); }

// Exercise the actual adapter and local provider, not only the recording double.
var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["PaymentProvider:ApprovedReply"] = "SUCCESS" }).Build();
var provider = new SimulatedProvider(config);
var http = new HttpClient(provider) { BaseAddress = new Uri("http://stockline-simulator.test") };
var adapter = new PaymentAdapter(new FixtureHttpFactory(http));
var realStore = new OrderStore();
var real = new OrdersController(new PlaceOrder(catalog, pricing, realStore, adapter), realStore);
await real.Place(new("CAB-105",2),default);
Check(provider.Events.Any(e => e.Action == "Authorized" && e.Amount == 15m), "Actual adapter/provider authorizes catalog amount 15");
var eventCount=provider.Events.Count;
await real.Place(new("UNKNOWN",2),default);
Check(provider.Events.Count == eventCount, "Unknown SKU produces no actual provider event");
// Direct application tests do not construct a controller or inspect MVC results.
var directStore = new OrderStore();
var directGateway = new RecordingGateway();
var workflow = new PlaceOrder(catalog, pricing, directStore, directGateway);
var placed = (OrderPlaced)await workflow.Execute(new("BRG-204", 10), default);
Check(placed.Order.Total == 108m && directGateway.LastAmount == 108m && directGateway.Calls == 1,
    "Direct workflow: one authorization for the captured wholesale amount");
Check(directStore.Find(placed.Order.Id) == placed.Order, "Direct workflow stores completed order");
foreach (var bad in new[] { new PlaceOrderCommand(null, 2), new("UNKNOWN", 2), new("BRG-204", 0), new("BRG-204", 101) })
    Check(await workflow.Execute(bad, default) is OrderRejected && directGateway.Calls == 1,
        $"Direct workflow rejects before payment: {bad}");
var nextDirect = (OrderPlaced)await workflow.Execute(new("BRG-204", 1), default);
Check(nextDirect.Order.Id == "ORD-1049", "Direct rejected commands create no orders");
foreach (var decision in new[] { PaymentDecision.Declined, PaymentDecision.Unknown })
{
    directGateway.Decision = decision;
    var outcome = (OrderPlaced)await workflow.Execute(new("CAB-105", 2), default);
    Check(outcome.Order.Status == (decision == PaymentDecision.Declined ? "Declined" : "PendingReconciliation"),
        $"Direct workflow preserves {decision}");
}
using var directCancellation = new CancellationTokenSource(); directCancellation.Cancel();
try { await workflow.Execute(new("CAB-105", 2), directCancellation.Token); throw new Exception("Direct cancellation ignored"); }
catch (OperationCanceledException) { Check(directGateway.LastToken == directCancellation.Token, "Direct workflow forwards cancellation"); }

Console.WriteLine($"Verified {passed} checks.");

sealed class RecordingGateway : IPaymentGateway
{
    public int Calls; public decimal LastAmount; public CancellationToken LastToken;
    public PaymentDecision Decision = PaymentDecision.Approved;
    public Task<PaymentDecision> AuthorizeAsync(string operationKey, decimal total, CancellationToken token)
    {
        Calls++; LastAmount=total; LastToken=token; token.ThrowIfCancellationRequested();
        return Task.FromResult(Decision);
    }
}
sealed class FixtureHttpFactory(HttpClient client) : IHttpClientFactory
{ public HttpClient CreateClient(string name) => client; }

