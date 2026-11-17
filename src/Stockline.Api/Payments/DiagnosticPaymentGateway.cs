public sealed class DiagnosticPaymentGateway(
    IPaymentGateway inner, IPaymentAttemptSink sink) : IPaymentGateway
{
    public async Task<PaymentDecision> AuthorizeAsync(
        string operationKey, decimal total, CancellationToken cancellationToken)
    {
        PaymentDecision decision;
        try
        {
            // One call, including when the token is already canceled. The inner
            // implementation owns its result and cancellation behavior.
            decision = await inner.AuthorizeAsync(operationKey, total, cancellationToken);
        }
        catch (OperationCanceledException exception)
        {
            TryRecord(operationKey, total, PaymentAttemptTermination.Canceled, null, exception);
            throw;
        }
        catch (Exception exception)
        {
            TryRecord(operationKey, total, PaymentAttemptTermination.Faulted, null, exception);
            throw;
        }
        TryRecord(operationKey, total, PaymentAttemptTermination.Returned, decision, null);
        return decision;
    }

    private void TryRecord(string operationKey, decimal amount, PaymentAttemptTermination termination,
        PaymentDecision? decision, Exception? exception)
    {
        try
        {
            sink.Record(new PaymentAttempt(Guid.NewGuid(), operationKey, amount, DateTimeOffset.UtcNow,
                termination, decision, exception?.GetType().FullName));
        }
        catch (Exception)
        {
            // Best-effort local diagnostics must not replace the inner outcome.
            // Recording can be lost on sink failure or process termination.
        }
    }
}
