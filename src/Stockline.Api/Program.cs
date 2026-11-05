var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddSingleton<OrderStore>();
builder.Services.AddSingleton<SimulatedProvider>();
builder.Services.AddHttpClient("provider", client => client.BaseAddress = new Uri("http://stockline-simulator.test"))
    .ConfigurePrimaryHttpMessageHandler(services => services.GetRequiredService<SimulatedProvider>())
    .SetHandlerLifetime(Timeout.InfiniteTimeSpan);
await using var app = builder.Build();

app.MapControllers();

await app.RunAsync();
