// Synchronous, process-local callback: no delivery, retry or cancellation contract.
public interface IOrderConfirmedReaction
{
    void OnConfirmed(OrderConfirmed order);
}
