using System.Text.Json.Serialization;

// One observed application invocation, not a durable provider transaction record.
public sealed record PaymentAttempt(
    Guid AttemptId,
    string OperationKey,
    decimal Amount,
    DateTimeOffset CompletedAtUtc,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PaymentAttemptTermination Termination,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] PaymentDecision? Decision,
    string? ExceptionType);

public enum PaymentAttemptTermination { Returned, Canceled, Faulted }
