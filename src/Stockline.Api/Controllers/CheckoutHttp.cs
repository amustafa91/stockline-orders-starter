using Microsoft.AspNetCore.Mvc;

internal static class CheckoutHttp
{
    public static IActionResult ToResult(Order order) => order.Status switch
    {
        "Confirmed" => new CreatedResult($"/orders/{order.Id}", order),
        "Declined" => new ConflictObjectResult(order),
        _ => new AcceptedResult($"/orders/{order.Id}", order)
    };
}
