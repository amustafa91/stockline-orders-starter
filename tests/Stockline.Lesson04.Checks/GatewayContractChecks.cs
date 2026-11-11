using Microsoft.Extensions.Configuration;

internal static class GatewayContractChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var config = new ConfigurationBuilder().Build();
        using var simulator = new SimulatedProvider(config);
        var adapter = new PaymentAdapter(new SimulatorClientFactory(simulator));
        var substitute = new ContractGateway();

        check(await KeepsPromise(adapter, scenario => simulator.Scenario = scenario),
            "Gateway contract: production adapter with local simulator");
        check(await KeepsPromise(substitute, scenario => substitute.Scenario = scenario),
            "Gateway contract: deterministic test substitute");
        check(!await KeepsPromise(new BrokenGateway(), _ => { }),
            "Gateway contract catches an implementation that always approves");
        check(simulator.Events.Any(e => e.Action == "ReplyLost" && e.Amount == 42m),
            "Unknown includes a lost reply after simulated authorization");
    }

    private static async Task<bool> KeepsPromise(IPaymentGateway gateway, Action<ProviderScenario> configure)
    {
        foreach (var (scenario, expected) in new[]
        {
            (ProviderScenario.Approve, PaymentDecision.Approved),
            (ProviderScenario.Decline, PaymentDecision.Declined),
            (ProviderScenario.AuthorizeThenDisconnect, PaymentDecision.Unknown)
        })
        {
            configure(scenario);
            if (await gateway.AuthorizeAsync($"contract-{scenario}", 42m, default) != expected)
                return false;
        }

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        try
        {
            await gateway.AuthorizeAsync("contract-cancelled", 42m, cancellation.Token);
            return false;
        }
        catch (OperationCanceledException)
        {
            return true;
        }
    }

    private sealed class ContractGateway : IPaymentGateway
    {
        public ProviderScenario Scenario { get; set; }

        public Task<PaymentDecision> AuthorizeAsync(string key, decimal amount, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Scenario switch
            {
                ProviderScenario.Approve => PaymentDecision.Approved,
                ProviderScenario.Decline => PaymentDecision.Declined,
                _ => PaymentDecision.Unknown
            });
        }
    }

    private sealed class BrokenGateway : IPaymentGateway
    {
        public Task<PaymentDecision> AuthorizeAsync(string key, decimal amount, CancellationToken cancellationToken) =>
            Task.FromResult(PaymentDecision.Approved);
    }

    private sealed class SimulatorClientFactory(SimulatedProvider simulator) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(simulator, disposeHandler: false)
        {
            BaseAddress = new Uri("http://stockline-simulator.test")
        };
    }
}
