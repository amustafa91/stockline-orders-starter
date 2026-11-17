public sealed class LocalOrderConfirmedPublisher(
    IEnumerable<IOrderConfirmedReaction> reactions, LocalOrderPreviewStore previews)
{
    // Registration order is the explicit sequential invocation order.
    private readonly IOrderConfirmedReaction[] _reactions = reactions.ToArray();

    public void Publish(OrderConfirmed order)
    {
        foreach (var reaction in _reactions)
        {
            try { reaction.OnConfirmed(order); }
            catch (Exception)
            {
                // Even a callback's cancellation exception is a preview failure,
                // not permission to undo confirmation or cancel the checkout result.
                try { previews.RecordFailure(order.OrderId, reaction.GetType().Name); }
                catch (Exception) { /* Best-effort failure visibility cannot replace the saved result. */ }
            }
        }
    }
}
