# Stockline Orders · Course starter

Build Stockline Orders alongside Practical .NET Architecture. Module 1, Lesson 1 starts with a checkout controller that knows the payment provider's reply codes.

## Before you start

You need basic C#, interfaces, async/await, and HTTP requests and responses. Install the .NET 10 SDK, VS Code with C# support, Postman, and Git. You should know the basics of finding and cloning a repository on GitHub.

## Clone the starter

Clone the repository and open its folder in VS Code:

~~~text
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
code .
~~~

You can also run **Git: Clone** from the VS Code command palette, paste the same repository URL, choose a destination folder, and open the cloned repository. Use the **main** branch to start the lesson. Open Stockline.Orders.slnx when your editor asks which solution to load.

Git must be installed locally. This repository is public, so a GitHub account is not required to clone it. The completed lesson is a separate checkpoint; begin with main to build along.

## Run the application

Run this command from the folder containing the solution.

~~~powershell
dotnet run --project ./src/Stockline.Api/Stockline.csproj -- --urls http://localhost:5375
~~~

Import postman/Stockline Orders.postman_collection.json into Postman and open Create Order. In Postman's lightweight client, paste this request into its URL field.

~~~text
curl --request POST 'http://localhost:5375/orders' --header 'Content-Type: application/json' --data '{"quantity":2}'
~~~

The first result is HTTP 201 with Confirmed. The synthetic order contains two units priced at USD 12 each, for USD 24 total. Get Last Order reads the order created by your latest request.

## What each folder does

| Location | Purpose |
| --- | --- |
| src/Stockline.Api/Controllers | Receives requests, validates input, and returns HTTP responses |
| src/Stockline.Api/Orders | Order data, payment decisions, and the in-memory order store |
| src/Stockline.Api/Payments | Local simulated payment provider |
| src/Stockline.Api/appsettings.json | Provider reply and scenario used in the lesson |
| src/Stockline.Api/Program.cs | Registers services and maps controllers |
| postman | Requests used in the course |

## Lesson 1 · One provider changes its wording

1. Send Create Order with ApprovedReply set to APPROVED.
2. Change ApprovedReply to SUCCESS in src/Stockline.Api/appsettings.json.
3. Stop and restart the API, then send the same request. The unchanged controller returns HTTP 202 and PendingReconciliation, even though the simulated provider approved payment.
4. Add Orders/IPaymentGateway.cs. Its AuthorizeAsync method accepts an operation key, amount, and cancellation token, and returns a PaymentDecision.
5. Add Payments/PaymentAdapter.cs. Move the provider HTTP call, reply mapping, and transport-error handling from the controller into this adapter. Translate both APPROVED and SUCCESS to PaymentDecision.Approved.
6. Inject IPaymentGateway into OrdersController. Register IPaymentGateway with PaymentAdapter in Program.cs.
7. Build, restart, and repeat Create Order. The result is HTTP 201 and Confirmed.

The completed checkpoint is on the lesson-01-complete branch of this repository.

## Try the other outcomes

Set PaymentProvider:Scenario in appsettings.json and restart.

| Scenario | ApprovedReply | Expected result |
| --- | --- | --- |
| Approve | APPROVED | HTTP 201, Confirmed |
| Approve | SUCCESS | Starter HTTP 202, completed checkpoint HTTP 201 |
| Decline | Either value | HTTP 409, Declined |
| AuthorizeThenDisconnect | Either value | HTTP 202, PendingReconciliation |

A transport failure leaves the provider outcome unknown. Durable reconciliation and safe replay are covered later. This fixture uses simulated payments and in-memory orders. Restarting clears the orders, and repeated POST requests create new demo orders.
