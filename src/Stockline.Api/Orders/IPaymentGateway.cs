/// <summary>Authorizes the captured order amount using its operation key.</summary>
public interface IPaymentGateway
{
    /// <remarks>
    /// Approved and Declined require a definitive reply. Unknown means the outcome
    /// is indeterminate, including when the provider may have authorized before a disconnect.
    /// Propagate caller cancellation to the caller and downstream operation.
    /// This contract does not promise that cancellation rolls back provider effects.
    /// </remarks>
    Task<PaymentDecision> AuthorizeAsync(string operationKey, decimal total, CancellationToken cancellationToken);
}
