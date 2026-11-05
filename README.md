# Stockline Orders · Lesson 1 starter

Watch Lesson 1 of Clean Architecture in .NET, then use this solution to repeat the refactor. The starting checkout action handles the payment provider directly. During the lesson you will move that dependency behind a payment interface.

## Prerequisites

- Skills: basic C#, interfaces, async/await, and HTTP requests and responses.
- Software: the .NET 10 SDK, a C# editor, and a terminal. The commands below use PowerShell.
- Check your SDK with `dotnet --version`. Open `Stockline.L01.slnx` in your editor.

## Find your way around

| Location | What you will find |
| --- | --- |
| `src/Stockline.Api/Program.cs` | Service registrations and controller routing |
| `src/Stockline.Api/Controllers/` | Checkout action, request validation, and HTTP response mapping |
| `src/Stockline.Api/Orders/` | Order model, payment decision, and in-memory store |
| `src/Stockline.Api/Payments/` | Local simulated payment provider |

## Run the starter

From the folder containing the solution:

```powershell
dotnet run --project ./src/Stockline.Api/Stockline.csproj -- --urls http://127.0.0.1:5359
```

In a second terminal:

```powershell
$r = Invoke-WebRequest -Method Post -Uri 'http://127.0.0.1:5359/orders' -ContentType 'application/json' -Body '{"quantity":2}'
$r.StatusCode
$r.Headers.Location
$r.Content
```

The simulator approves the payment and loses the reply. Expect HTTP 202 with `PendingReconciliation`. Restart the API and reuse the same request after each checkpoint. The simulator and order store run in memory.

## Follow the refactor

1. Add `IPaymentGateway.cs` in `Orders/`.
2. Add `ProviderAdapter.cs` in `Payments/`, register it in `Program.cs`, and inject the interface into `OrdersController`.
3. Add `PlaceOrder.cs` in `Orders/`. Register it in `Program.cs` and call it from the controller's POST action.
