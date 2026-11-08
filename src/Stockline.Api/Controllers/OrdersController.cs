using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderStore store, IPaymentGateway payments, ProductCatalog catalog)
    : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Place(
        [FromBody] CheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!request.IsValid)
            return BadRequest(new { error = "Provide a SKU and quantity from 1 to 100." });

        var product = catalog.Find(request.Sku!);
        if (product is null)
            return BadRequest(new { error = "Unknown product SKU." });
        var order = store.Start(product, request.Quantity);
        var decision = await payments.AuthorizeAsync(
            order.OperationKey, order.Total, cancellationToken);
        return CheckoutHttp.ToResult(store.Complete(order, decision));
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id) =>
        store.Find(id) is { } order ? Ok(order) : NotFound();
}
