public sealed class ReceiptPreviewReaction(LocalOrderPreviewStore previews, bool failBeforePreview = false) : IOrderConfirmedReaction
{
    public void OnConfirmed(OrderConfirmed order)
    {
        if (failBeforePreview)
            throw new InvalidOperationException("Deliberate local receipt-preview failure after confirmation.");
        previews.SaveReceipt(order);
    }
}

public sealed class FulfillmentPreviewReaction(LocalOrderPreviewStore previews) : IOrderConfirmedReaction
{
    public void OnConfirmed(OrderConfirmed order) => previews.SaveFulfillment(order);
}
