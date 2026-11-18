using Microsoft.Extensions.DependencyInjection;

internal static class RequestDispatchChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var expected = new OrderRejected("fixture outcome");
        var resultTask = Task.FromResult<PlaceOrderResult>(expected);
        var handler = new Probe((_, _) => resultTask);
        var dispatcher = new LocalCheckoutDispatcher([handler]);
        var command = new PlaceOrderCommand("CAB-105", 10, "trade");
        using var caller = new CancellationTokenSource();
        var sent = dispatcher.Send(command, caller.Token);
        check(ReferenceEquals(sent, resultTask), "Dispatcher returns the original handler task");
        check(ReferenceEquals(await sent, expected), "Dispatcher preserves the original outcome instance");
        check(handler.Calls == 1 && ReferenceEquals(handler.Command, command), "One request reaches one handler exactly once with the same command");
        check(handler.Token == caller.Token, "Dispatcher forwards the original cancellation token");
        await dispatcher.Send(command, caller.Token);
        check(handler.Calls == 2, "A second explicit Send invokes again; no deduplication is promised");

        foreach (var count in new[] { 0, 2 })
        {
            var probe = new Probe((_, _) => resultTask);
            try { _ = new LocalCheckoutDispatcher(Enumerable.Repeat<IPlaceOrderHandler>(probe, count)); throw new Exception("Missing cardinality rejection"); }
            catch (InvalidOperationException ex)
            {
                check(ex.Message.Contains($"found {count}"), $"Composition rejects {count} handlers explicitly");
                check(probe.Calls == 0, "Invalid composition performs no checkout work");
            }
        }
        var original = new InvalidOperationException("original handler failure");
        var faulted = new Probe((_, _) => Task.FromException<PlaceOrderResult>(original));
        try { await new LocalCheckoutDispatcher([faulted]).Send(command, caller.Token); throw new Exception("Missing failure"); }
        catch (InvalidOperationException ex) { check(ReferenceEquals(ex, original) && faulted.Calls == 1, "Async handler failure propagates unchanged without retry"); }
        var synchronous = new Probe((_, _) => throw original);
        try { _ = new LocalCheckoutDispatcher([synchronous]).Send(command, caller.Token); throw new Exception("Missing synchronous failure"); }
        catch (InvalidOperationException ex) { check(ReferenceEquals(ex, original) && synchronous.Calls == 1, "Synchronous handler failure propagates unchanged"); }
        using var cancelled = new CancellationTokenSource();cancelled.Cancel();
        var cancelledTask = Task.FromCanceled<PlaceOrderResult>(cancelled.Token);
        var cancelHandler = new Probe((_, _) => cancelledTask);
        var cancelResult = new LocalCheckoutDispatcher([cancelHandler]).Send(command, cancelled.Token);
        check(ReferenceEquals(cancelResult, cancelledTask), "Cancellation task is forwarded unchanged");
        try { await cancelResult; throw new Exception("Missing cancellation"); }
        catch (OperationCanceledException ex) { check(ex.CancellationToken == cancelled.Token && cancelHandler.Calls == 1, "Cancellation retains its original token and one invocation"); }

        var services = new ServiceCollection();
        services.AddScoped<ScopedProbe>();
        services.AddScoped<IPlaceOrderHandler>(sp => sp.GetRequiredService<ScopedProbe>());
        services.AddScoped<LocalCheckoutDispatcher>();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var first = provider.CreateScope();using var second = provider.CreateScope();
        var a = first.ServiceProvider.GetRequiredService<LocalCheckoutDispatcher>();
        await a.Send(command, caller.Token);
        check(first.ServiceProvider.GetRequiredService<ScopedProbe>().Calls == 1, "Scoped dispatcher uses the registered scoped handler instance");
        check(!ReferenceEquals(a, second.ServiceProvider.GetRequiredService<LocalCheckoutDispatcher>()), "Dispatchers are not shared across request scopes");
    }
    private sealed class Probe(Func<PlaceOrderCommand, CancellationToken, Task<PlaceOrderResult>> action) : IPlaceOrderHandler
    {
        public int Calls;public PlaceOrderCommand? Command;public CancellationToken Token;
        public Task<PlaceOrderResult> Execute(PlaceOrderCommand command, CancellationToken token) { Calls++;Command=command;Token=token;return action(command,token); }
    }
    private sealed class ScopedProbe : IPlaceOrderHandler
    {
        public int Calls;
        public Task<PlaceOrderResult> Execute(PlaceOrderCommand command, CancellationToken token) { Calls++;return Task.FromResult<PlaceOrderResult>(new OrderRejected("scope fixture")); }
    }
}
