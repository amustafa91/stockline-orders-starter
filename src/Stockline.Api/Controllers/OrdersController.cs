using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderStore store, IHttpClientFactory clients)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(
        [FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsValid) return BadRequest();
        var order = store.Start(request.Quantity);
        using var client = clients.CreateClient("provider");
        PaymentDecision decision;
        try
        {
            using var response = await client.PostAsJsonAsync("authorize",
                new ProviderRequest(order.OperationKey, order.Total), cancellationToken);
            response.EnsureSuccessStatusCode();
            var reply = await response.Content.ReadFromJsonAsync<ProviderReply>(cancellationToken);
            decision = reply?.Code switch
            {
                "APPROVED" => PaymentDecision.Approved,
                "DECLINED" => PaymentDecision.Declined,
                _ => PaymentDecision.Unknown
            };
        }
        catch (HttpRequestException)
        {
            decision = PaymentDecision.Unknown;
        }
        return CheckoutHttp.ToResult(store.Complete(order, decision));
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        store.Find(id) is { } order ? Ok(order) : NotFound();
}
