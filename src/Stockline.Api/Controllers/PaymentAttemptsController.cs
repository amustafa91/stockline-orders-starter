using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("diagnostics/payment-attempts")]
public sealed class PaymentAttemptsController(
    LocalPaymentAttemptSink sink, IWebHostEnvironment environment) : ControllerBase
{
    [HttpGet]
    public IActionResult Get([FromQuery] string? operationKey = null)
    {
        // This unauthenticated teaching view is available only in Development.
        if (!environment.IsDevelopment()) return NotFound();
        if (operationKey?.Length > LocalPaymentAttemptSink.MaximumTextLength)
            return BadRequest(new { error = "Operation key must be at most 128 characters." });
        return Ok(sink.Snapshot(operationKey));
    }
}
