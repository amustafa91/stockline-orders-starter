# Stockline Orders · Lesson 1 completed checkpoint

This branch contains the completed payment-adapter refactor. The main branch contains the course starter. The steps below describe the changes demonstrated in Lesson 1. SUCCESS is configured here and returns HTTP 201 with Confirmed.

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
dotnet run --project ./src/Stockline.Api/Stockline.csproj
~~~

The local **Stockline** launch profile in `src/Stockline.Api/Properties/launchSettings.json` binds the API to **http://localhost:7375**, matching the collection's `baseUrl` variable. No certificate setup, extra URL argument, or Postman environment is needed. You can select the profile explicitly with `--launch-profile Stockline`. This HTTP address is for the local course exercise.

Import `postman/Stockline Orders.postman_collection.json` into Postman. In **Stockline Orders → Module 01 - Clean Code Foundations → Lesson 01 - Isolate the Payment Provider**, open **Create Order**. In Postman's lightweight client, paste this request into its URL field.

~~~text
curl --request POST 'http://localhost:7375/orders' --header 'Content-Type: application/json' --data '{"quantity":2}'
~~~

The result is HTTP 201 with Confirmed. The synthetic order contains two units priced at USD 12 each, for USD 24 total. Get Last Order reads the order created by your latest request.

Opening `/` in a browser returns 404: this API has no homepage. Use **POST /orders** with the JSON body shown above. If port 7375 is occupied, stop your other Stockline instance; or choose another HTTP port with `-- --urls http://localhost:7376` and change the collection's `baseUrl` to match. The launch profile is for local development and is not deployed. The setup and Postman collection use the same local address; the exercise and expected results are unchanged.

## Postman course folders

Import the single `postman/Stockline Orders.postman_collection.json` file. All code checkpoints use the same collection and folder structure:

```text
Stockline Orders
└── Module 01 - Clean Code Foundations
    ├── Lesson 01 - Isolate the Payment Provider
    │   ├── Create Order
    │   └── Get Last Order
    └── Lesson 02 - Names That Reveal Intent
        ├── Create Order - two units
        ├── Create Order - one unit
        ├── Create Order - three units
        ├── Get Last Order
        └── Reject zero quantity
```

Open or run only the current lesson folder against its matching code. Lesson 1 uses USD 12 per item; the completed Lesson 2 fixture uses 199 cents per item. A separate saved order identifier for each lesson prevents one lesson's request from replacing the other's value. Send a Create Order request before Get Last Order; restarting the API clears stored orders. The collection-level `baseUrl` is **http://localhost:7375** for every lesson. Future lessons belong under this collection, rather than separate collections.

## What each folder does

| Location | Purpose |
| --- | --- |
| src/Stockline.Api/Controllers | Receives requests, validates input, and returns HTTP responses |
| src/Stockline.Api/Orders | Order data, payment decisions, and the in-memory order store |
| src/Stockline.Api/Payments | Local simulated payment provider |
| src/Stockline.Api/appsettings.json | Provider reply and scenario used in the lesson |
| src/Stockline.Api/Program.cs | Registers services and maps controllers |
| src/Stockline.Api/Properties/launchSettings.json | Local development URL shared with Postman |
| postman | Requests used in the course |

Comments explain intentional teaching shortcuts and decisions. They do not replace the lesson or describe every statement. The starter intentionally keeps the Lesson 1 problem; the `lesson-01-complete` branch contains its solution. After completing Lesson 1, use your own working copy for Lesson 2, or switch to `lesson-01-complete` to begin from the matching checkpoint.

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
