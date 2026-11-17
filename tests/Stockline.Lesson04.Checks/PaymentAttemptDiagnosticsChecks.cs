using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using System.Text.Json;

internal static class PaymentAttemptDiagnosticsChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        foreach (var expected in Enum.GetValues<PaymentDecision>())
        {
            using var cancellation = new CancellationTokenSource();
            var sink = new LocalPaymentAttemptSink();
            var inner = new ProbeGateway((_, _, _) => Task.FromResult(expected));
            var before = DateTimeOffset.UtcNow;
            var actual = await new DiagnosticPaymentGateway(inner, sink)
                .AuthorizeAsync("authorize:fixture", 63.75m, cancellation.Token);
            var entry = sink.Snapshot().Single();
            check(actual == expected && inner.Calls == 1 && inner.Key == "authorize:fixture"
                && inner.Total == 63.75m && inner.Token == cancellation.Token,
                $"Decorator forwards exact arguments once and preserves {expected}");
            check(entry.Amount == inner.Total && entry.Amount == 63.75m,
                $"Returned {expected} observation retains the exact amount forwarded to the gateway");
            check(entry.Decision == expected && entry.Termination == PaymentAttemptTermination.Returned
                && entry.ExceptionType is null && entry.AttemptId != Guid.Empty
                && entry.OperationKey == inner.Key && entry.CompletedAtUtc >= before
                && entry.CompletedAtUtc <= DateTimeOffset.UtcNow,
                $"One bounded observation describes returned {expected}");
        }

        foreach (var asynchronous in new[] { false, true })
        {
            var error = new InvalidOperationException("private failure detail must not be recorded");
            var inner = new ProbeGateway((_, _, _) => asynchronous
                ? Task.FromException<PaymentDecision>(error) : throw error);
            var sink = new LocalPaymentAttemptSink();
            var observed = await Failure(new DiagnosticPaymentGateway(inner, sink)
                .AuthorizeAsync("failure", 10m, default));
            var entry = sink.Snapshot().Single();
            check(ReferenceEquals(observed, error) && inner.Calls == 1,
                $"Decorator preserves original {(asynchronous ? "asynchronous" : "synchronous")} exception");
            check(entry.Termination == PaymentAttemptTermination.Faulted && entry.Decision is null && entry.Amount == 10m
                && entry.ExceptionType == typeof(InvalidOperationException).FullName
                && !JsonSerializer.Serialize(entry).Contains(error.Message),
                "Fault diagnostics retain type only, without private exception detail");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            var sink = new LocalPaymentAttemptSink();
            var inner = new ProbeGateway((_, _, token) =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult(PaymentDecision.Approved);
            });
            var task = new DiagnosticPaymentGateway(inner, sink)
                .AuthorizeAsync("pre-canceled", 10m, cancellation.Token);
            var observed = await Failure(task);
            check(observed is OperationCanceledException error && error.CancellationToken == cancellation.Token
                && inner.Calls == 1 && inner.Token == cancellation.Token && task.IsCanceled,
                "Pre-canceled invocation still reaches the inner gateway once with the caller token");
            check(sink.Snapshot().Single().Termination == PaymentAttemptTermination.Canceled && sink.Snapshot()[0].Amount == 10m,
                "Cancellation is observed without claiming provider rollback");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var inner = new ProbeGateway(async (_, _, token) =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.Infinite, token);
                return PaymentDecision.Approved;
            });
            var sink = new LocalPaymentAttemptSink();
            var task = new DiagnosticPaymentGateway(inner, sink)
                .AuthorizeAsync("in-flight", 10m, cancellation.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            cancellation.Cancel();
            var observed = await Failure(task);
            check(observed is OperationCanceledException error && error.CancellationToken == cancellation.Token
                && inner.Calls == 1 && sink.Snapshot().Single().Termination == PaymentAttemptTermination.Canceled && sink.Snapshot()[0].Amount == 10m,
                "In-flight cancellation reaches the actual inner operation");
        }

        using (var cancellation = new CancellationTokenSource())
        {
            var sink = new LocalPaymentAttemptSink();
            cancellation.Cancel();
            var inner = new ProbeGateway((_, _, _) => Task.FromResult(PaymentDecision.Approved));
            var result = await new DiagnosticPaymentGateway(inner, sink)
                .AuthorizeAsync("canceled-but-returned", 10m, cancellation.Token);
            check(result == PaymentDecision.Approved && inner.Calls == 1
                && sink.Snapshot().Single().Termination == PaymentAttemptTermination.Returned,
                "The decorator does not substitute cancellation for an inner approval");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            var inner = new ProbeGateway((_, _, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult(PaymentDecision.Approved);
            });
            check(await new DiagnosticPaymentGateway(inner, new LocalPaymentAttemptSink())
                .AuthorizeAsync("completion-race", 10m, cancellation.Token) == PaymentDecision.Approved,
                "Cancellation racing with completion cannot overwrite the returned decision");
        }

        foreach (var decision in Enum.GetValues<PaymentDecision>())
        {
            var sink = new ThrowingSink();
            var inner = new ProbeGateway((_, _, _) => Task.FromResult(decision));
            check(await new DiagnosticPaymentGateway(inner, sink).AuthorizeAsync("sink-failure", 10m, default) == decision
                && sink.Calls == 1 && inner.Calls == 1,
                $"Diagnostic sink failure cannot replace {decision} or trigger a retry");
        }
        using (var cancellation = new CancellationTokenSource())
        {
            cancellation.Cancel();
            foreach (var error in new Exception[]
            {
                new InvalidOperationException("original"),
                new OperationCanceledException("original cancellation", cancellation.Token)
            })
            {
                var sink = new ThrowingSink();
                var inner = new ProbeGateway((_, _, _) => Task.FromException<PaymentDecision>(error));
                var observed = await Failure(new DiagnosticPaymentGateway(inner, sink)
                    .AuthorizeAsync("sink-failure", 10m, cancellation.Token));
                check(ReferenceEquals(observed, error) && sink.Calls == 1 && inner.Calls == 1,
                    $"Diagnostic sink failure cannot mask {error.GetType().Name}");
            }
        }

        var repeated = new LocalPaymentAttemptSink();
        var repeatedInner = new ProbeGateway((_, _, _) => Task.FromResult(PaymentDecision.Unknown));
        var repeatedGateway = new DiagnosticPaymentGateway(repeatedInner, repeated);
        await repeatedGateway.AuthorizeAsync("same-key", 10m, default);
        await repeatedGateway.AuthorizeAsync("same-key", 10m, default);
        check(repeated.Snapshot().Length == 2 && repeated.Snapshot().Select(a => a.AttemptId).Distinct().Count() == 2
            && repeatedInner.Calls == 2,
            "Repeated operation keys remain separate invocation observations without deduplication");

        var bounded = new LocalPaymentAttemptSink(3);
        var entries = Enumerable.Range(0, 4).Select(i => Entry("key-" + i)).ToArray();
        foreach (var entry in entries) bounded.Record(entry);
        check(bounded.Snapshot().SequenceEqual(entries.Skip(1)),
            "The oldest observation is evicted at capacity, retaining recording order");
        var snapshot = bounded.Snapshot();
        snapshot[0] = Entry("changed snapshot");
        check(bounded.Snapshot()[0] == entries[1], "Snapshot mutation cannot change retained observations");
        check(bounded.Snapshot("key-2").Single() == entries[2]
            && bounded.Snapshot("missing").Length == 0 && bounded.Snapshot().Length == 3,
            "Filtered reads are bounded and do not clear retained observations");
        foreach (var capacity in new[] { 0, -1, 101 })
        {
            try { _ = new LocalPaymentAttemptSink(capacity); check(false, "Invalid capacity must fail"); }
            catch (ArgumentOutOfRangeException) { check(true, $"Capacity {capacity} is rejected"); }
        }
        var longKey = new string('k', 1000);
        var longInner = new ProbeGateway((_, _, _) => Task.FromResult(PaymentDecision.Approved));
        await new DiagnosticPaymentGateway(longInner, bounded).AuthorizeAsync(longKey, 10m, default);
        bounded.Record(Entry(longKey) with { ExceptionType = new string('e', 1000) });
        check(longInner.Key == longKey && bounded.Snapshot().Last().OperationKey.Length == 128
            && bounded.Snapshot().Last().ExceptionType!.Length == 128,
            "Retention bounds strings without altering the original inner input");
        var concurrent = new LocalPaymentAttemptSink();
        Parallel.For(0, 1000, i =>
        {
            concurrent.Record(Entry("concurrent-" + i));
            if (concurrent.Snapshot().Length > 100) throw new Exception("Unbounded concurrent snapshot");
        });
        check(concurrent.Snapshot().Length == 100 && concurrent.Snapshot().Select(a => a.AttemptId).Distinct().Count() == 100,
            "Concurrent writers and snapshots retain at most 100 complete observations");

        var controller = new PaymentAttemptsController(bounded, new TestEnvironment("Development"));
        var response = (OkObjectResult)controller.Get();
        check(((PaymentAttempt[])response.Value!).Length == 3,
            "Development endpoint returns the shared bounded snapshot");
        check(((PaymentAttempt[])((OkObjectResult)controller.Get("missing")).Value!).Length == 0,
            "Development endpoint returns an empty array for no matching operation");
        check(controller.Get(new string('k', 129)) is BadRequestObjectResult,
            "Diagnostic endpoint rejects overlong operation filters");
        check(new PaymentAttemptsController(bounded, new TestEnvironment("Production")).Get() is NotFoundResult
            && new PaymentAttemptsController(bounded, new TestEnvironment("Staging")).Get(longKey) is NotFoundResult,
            "Diagnostic reads return 404 outside Development, including invalid filters");
        var json = JsonSerializer.Serialize(repeated.Snapshot()[0], new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);
        check(document.RootElement.GetProperty("amount").GetDecimal() == 10m
            && document.RootElement.GetProperty("termination").GetString() == "Returned"
            && document.RootElement.GetProperty("decision").GetString() == "Unknown",
            "Postman receives readable termination and decision names");
    }

    private static PaymentAttempt Entry(string key) => new(Guid.NewGuid(), key, 10m, DateTimeOffset.UtcNow,
        PaymentAttemptTermination.Returned, PaymentDecision.Approved, null);

    private static async Task<Exception?> Failure(Task<PaymentDecision> task)
    {
        try { await task; return null; }
        catch (Exception exception) { return exception; }
    }

    private sealed class ProbeGateway(Func<string, decimal, CancellationToken, Task<PaymentDecision>> invoke) : IPaymentGateway
    {
        public int Calls;
        public string? Key;
        public decimal Total;
        public CancellationToken Token;
        public Task<PaymentDecision> AuthorizeAsync(string operationKey, decimal total, CancellationToken cancellationToken)
        {
            Calls++; Key = operationKey; Total = total; Token = cancellationToken;
            return invoke(operationKey, total, cancellationToken);
        }
    }

    private sealed class ThrowingSink : IPaymentAttemptSink
    {
        public int Calls;
        public void Record(PaymentAttempt attempt) { Calls++; throw new InvalidOperationException("diagnostics unavailable"); }
    }

    private sealed class TestEnvironment(string name) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "Stockline.Checks";
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
