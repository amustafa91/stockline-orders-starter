var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});
builder.Services.AddControllers();
builder.Services.AddStocklineCheckout();
await using var app = builder.Build();

app.MapControllers();

await app.RunAsync();
