# Stockline Orders - Module 3 Lesson 4: One Request, One Handler

This is the starting checkpoint. Start on a practice branch from [`module-03-lesson-04-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-04-start); compare with [`module-03-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-04-complete) after your implementation. Earlier Lessons 1 and 2 are preserved.

## Get the code

Prerequisites: .NET 10 SDK, Git, VS Code with C# support and Postman. Git tracks local work; GitHub hosts this public repository. Reading/cloning it does not require a GitHub account. A normal clone begins on main, the Module 1 starter.

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git status
git fetch origin
git switch -c my-module-03-lesson-04 origin/module-03-lesson-04-start
```

For an existing checkout, commit or stash unfinished work including untracked files before switching. Use a fresh practice-branch name if that name exists. Open Stockline.Orders.slnx.

| Lesson | Start | Completed comparison |
|---|---|---|
| 1 - Decorator diagnostics | module-03-lesson-01-start | module-03-lesson-01-complete |
| 2 - Observer reactions | module-03-lesson-01-complete | module-03-lesson-02-complete |
| 3 - Factory and eligibility | module-03-lesson-03-start | module-03-lesson-03-complete |
| 4 - One-handler mediator | module-03-lesson-04-start | module-03-lesson-04-complete |


## Baseline and completed behavior

The starting application is completed Lesson 3, including the factory and pricing eligibility. Establish direct-controller standard CAB10=63.75 and trade CAB10=60 before changing dispatch.

This is a small domain-specific mediator comparison, not a mandatory framework migration. IPlaceOrderHandler retains the existing Execute(command,token) contract. LocalCheckoutDispatcher requires exactly one registered handler, forwards the original command/token once and returns its original task/result. Zero or multiple handlers fail explicitly. The dispatcher and handler remain scoped; the interface aliases the same PlaceOrder instance. OrdersController sends through the dispatcher and retains the same HTTP mapping. The workflow body, pricing factory, diagnostic decorator and saved confirmation are unchanged.

Send asks one handler to do work and expects one result. Publish announces OrderConfirmed after saving and invokes zero or more optional local subscribers. A subscriber failure is isolated; a request-handler failure propagates. The dispatcher adds no retry, durable queue, deduplication or exactly-once guarantee. Direct calls remain simpler for one stable operation without shared dispatch needs. No mediator package is required.

The completed HTTP results remain standard63.75, trade60, trade9=57.38, unknown/inactive400, decline409 and disconnect202/PendingReconciliation. Confirmed orders have two local previews and one corresponding payment attempt; declined/unknown orders have no confirmed-order previews.

## Run locally

```powershell
dotnet build Stockline.Orders.slnx
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:PaymentProvider__Scenario = 'Approve'
$env:PaymentProvider__ApprovedReply = 'SUCCESS'
$env:OrderPreviews__FailReceipt = 'false'
dotnet run --project src/Stockline.Api --no-launch-profile -- --urls http://127.0.0.1:7376
```

Import postman/Stockline Orders.postman_collection.json. Set baseUrl=http://127.0.0.1:7376. Open Module03 / Lesson04. Use the default-standard request for the baseline; additional completed expectations apply after your implementation. Each fresh POST saves its returned order ID and operationKey as collection variables. Read the saved order and previews by order ID, and payment attempts by operationKey in the same API process. Restarting clears local stores. Receipt total and payment amount match saved total; fulfillment shows product and quantity. These are local previews, not email or shipments. Development read endpoints are hidden in Production.

## Checks after implementation

Before edits, the starting checkpoint passes161 checks. The completed checkpoint passes176. New constructor dependencies require the matching test fixtures; after writing your own application changes, fetch and restore only the supplied checks, preserving your edited src files:

```powershell
git fetch origin
git restore --source origin/module-03-lesson-04-complete -- tests/Stockline.Lesson04.Checks
dotnet run --project tests/Stockline.Lesson04.Checks -- --dispatch-only
dotnet run --project tests/Stockline.Lesson04.Checks
```

Only restore that checks folder, not the full completed application. The historical project name Stockline.Lesson04.Checks is retained so earlier commands stay compatible. The complete source is for comparison after your attempt. The video opens actual retained check output in the editor; this is not a live terminal recording.
