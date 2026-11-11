public static class CheckoutRegistration
{
    public static IServiceCollection AddStocklineCheckout(this IServiceCollection services)
    {
        services.AddScoped<IPaymentGateway, PaymentAdapter>();
        services.AddSingleton<OrderStore>();
        services.AddSingleton<ProductCatalog>();
        services.AddSingleton<IDiscountPolicy, WholesaleDiscount>();
        services.AddSingleton<IDiscountPolicy, CableCampaign>();
        services.AddSingleton<OrderPricing>();
        services.AddScoped<PlaceOrder>();
        services.AddSingleton<SimulatedProvider>();
        // This handler simulates a provider locally; the .test address never reaches a payment service.
        services.AddHttpClient("provider", client => client.BaseAddress = new Uri("http://stockline-simulator.test"))
            .ConfigurePrimaryHttpMessageHandler(services => services.GetRequiredService<SimulatedProvider>())
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        return services;
    }
}
