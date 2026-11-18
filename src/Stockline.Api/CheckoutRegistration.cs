public static class CheckoutRegistration
{
    public static IServiceCollection AddStocklineCheckout(this IServiceCollection services)
    {
        services.AddScoped<PaymentAdapter>();
        services.AddSingleton<LocalPaymentAttemptSink>();
        services.AddSingleton<IPaymentAttemptSink>(services =>
            services.GetRequiredService<LocalPaymentAttemptSink>());
        services.AddScoped<IPaymentGateway>(services => new DiagnosticPaymentGateway(
            services.GetRequiredService<PaymentAdapter>(),
            services.GetRequiredService<IPaymentAttemptSink>()));
        services.AddSingleton<OrderStore>();
        services.AddSingleton<ProductCatalog>();
        services.AddSingleton<CustomerCatalog>();
        services.AddSingleton<CustomerDiscountPolicyFactory>();
        services.AddSingleton<IDiscountPolicy, WholesaleDiscount>();
        services.AddSingleton<IDiscountPolicy, CableCampaign>();
        services.AddSingleton<OrderPricing>();
        services.AddSingleton<LocalOrderPreviewStore>();
        services.AddSingleton<IOrderConfirmedReaction>(services => new ReceiptPreviewReaction(
            services.GetRequiredService<LocalOrderPreviewStore>(),
            services.GetRequiredService<IHostEnvironment>().IsDevelopment()
                && services.GetRequiredService<IConfiguration>().GetValue<bool>("OrderPreviews:FailReceipt")));
        services.AddSingleton<IOrderConfirmedReaction, FulfillmentPreviewReaction>();
        services.AddSingleton<LocalOrderConfirmedPublisher>();
        services.AddScoped<PlaceOrder>();
        services.AddScoped<IPlaceOrderHandler>(services => services.GetRequiredService<PlaceOrder>());
        services.AddScoped<LocalCheckoutDispatcher>();
        services.AddSingleton<SimulatedProvider>();
        // This handler simulates a provider locally; the .test address never reaches a payment service.
        services.AddHttpClient("provider", client => client.BaseAddress = new Uri("http://stockline-simulator.test"))
            .ConfigurePrimaryHttpMessageHandler(services => services.GetRequiredService<SimulatedProvider>())
            .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        return services;
    }
}
