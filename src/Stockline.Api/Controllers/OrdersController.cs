using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderStore store, IPaymentGateway payments)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(
        [FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsValid) return BadRequest();
        var order = store.Start(request.Quantity);
        var decision = await payments.AuthorizeAsync(
            order.OperationKey, order.Total, cancellationToken);
        return CheckoutHttp.ToResult(store.Complete(order, decision));
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        store.Find(id) is { } order ? Ok(order) : NotFound();
}
