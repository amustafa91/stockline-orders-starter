using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("orders/{orderId}/previews")]
public sealed class OrderPreviewsController(
    LocalOrderPreviewStore previews, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult Get(string orderId)
    {
        if (!environment.IsDevelopment()) return NotFound();
        return previews.Find(orderId) is { } state ? Ok(state) : NotFound();
    }
}
