using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("orders")]
public sealed class OrdersController(PlaceOrder placeOrder, OrderStore store)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(
        [FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        var result = await placeOrder.Execute(
            new PlaceOrderCommand(request.Sku, request.Quantity), cancellationToken);
        return result switch
        {
            OrderRejected rejected => BadRequest(new { error = rejected.Error }),
            OrderPlaced placed => CheckoutHttp.ToResult(placed.Order),
            _ => throw new InvalidOperationException("Unexpected checkout result.")
        };
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        store.Find(id) is { } order ? Ok(order) : NotFound();
}
