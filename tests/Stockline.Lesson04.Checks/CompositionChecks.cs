using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

internal static class CompositionChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddStocklineCheckout();
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        await using var first = provider.CreateAsyncScope();
        await using var second = provider.CreateAsyncScope();
        var firstWorkflow = first.ServiceProvider.GetRequiredService<PlaceOrder>();
        check(firstWorkflow == first.ServiceProvider.GetRequiredService<PlaceOrder>()
            && firstWorkflow != second.ServiceProvider.GetRequiredService<PlaceOrder>(),
            "PlaceOrder is scoped per request");
        var firstGateway = first.ServiceProvider.GetRequiredService<IPaymentGateway>();
        check(firstGateway is PaymentAdapter
            && firstGateway == first.ServiceProvider.GetRequiredService<IPaymentGateway>()
            && firstGateway != second.ServiceProvider.GetRequiredService<IPaymentGateway>(),
            "The production gateway is a scoped adapter");
        check(first.ServiceProvider.GetRequiredService<OrderStore>() == second.ServiceProvider.GetRequiredService<OrderStore>()
            && first.ServiceProvider.GetRequiredService<OrderPricing>() == second.ServiceProvider.GetRequiredService<OrderPricing>(),
            "Order storage and stateless pricing are shared across scopes");

        var result = (OrderPlaced)await firstWorkflow.Execute(new("CAB-105", 10), default);
        check(result.Order.Total == 63.75m && result.Order.Status == "Confirmed",
            "The production graph resolves and authorizes the selected offer");

        var substitute = new RecordingGateway();
        services.RemoveAll<IPaymentGateway>();
        services.AddScoped<IPaymentGateway>(_ => substitute);
        await using var testProvider = services.BuildServiceProvider();
        await using var testScope = testProvider.CreateAsyncScope();
        var substituted = testScope.ServiceProvider.GetRequiredService<PlaceOrder>();
        var testResult = (OrderPlaced)await substituted.Execute(new("CAB-105", 10), default);
        check(testResult.Order.Total == substitute.LastAmount && substitute.Calls == 1,
            "Changing composition substitutes the gateway without workflow edits");

        var invalid = new ServiceCollection();
        invalid.AddScoped<ScopedValue>();
        invalid.AddSingleton<CaptiveConsumer>();
        try
        {
            using var broken = invalid.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });
            check(false, "Scope validation must reject a singleton capturing a scoped service");
        }
        catch (AggregateException exception)
        {
            check(exception.InnerExceptions.Any(error => error.Message.Contains("Cannot consume scoped service")),
                "Scope validation rejects a captive dependency");
        }
    }

    private sealed class ScopedValue;
    private sealed class CaptiveConsumer(ScopedValue value)
    {
        public ScopedValue Value { get; } = value;
    }
}
