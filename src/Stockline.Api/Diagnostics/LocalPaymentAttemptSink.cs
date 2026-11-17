public sealed class LocalPaymentAttemptSink : IPaymentAttemptSink
{
    public const int MaximumCapacity = 100;
    public const int MaximumTextLength = 128;
    private readonly object _gate = new();
    private readonly Queue<PaymentAttempt> _attempts = new();
    public int Capacity { get; }

    public LocalPaymentAttemptSink(int capacity = MaximumCapacity)
    {
        if (capacity is < 1 or > MaximumCapacity)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
    }

    public void Record(PaymentAttempt attempt)
    {
        // Bound strings as well as entry count. Original gateway inputs are untouched.
        var bounded = attempt with
        {
            OperationKey = Bound(attempt.OperationKey),
            ExceptionType = attempt.ExceptionType is null ? null : Bound(attempt.ExceptionType)
        };
        lock (_gate)
        {
            if (_attempts.Count == Capacity) _attempts.Dequeue();
            _attempts.Enqueue(bounded);
        }
    }

    // A detached snapshot in recording order, which may differ from invocation order.
    public PaymentAttempt[] Snapshot(string? operationKey = null)
    {
        lock (_gate)
            return _attempts.Where(a => operationKey is null || a.OperationKey == operationKey).ToArray();
    }

    private static string Bound(string value) =>
        value.Length <= MaximumTextLength ? value : value[..MaximumTextLength];
}
