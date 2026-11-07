public interface IPaymentGateway
{
    Task<PaymentDecision> AuthorizeAsync(
        string operationKey, decimal amount, CancellationToken cancellationToken);
}
