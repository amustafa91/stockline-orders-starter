public interface IPaymentAttemptSink
{
    void Record(PaymentAttempt attempt);
}
