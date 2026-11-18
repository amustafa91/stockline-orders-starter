# Stockline Orders - Module 3 Lesson 3: Choose the Policy, Then Check Eligibility

This is the completed checkpoint. Start on a practice branch from [`module-03-lesson-03-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-03-start); compare with [`module-03-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-03-complete) after your implementation. Earlier Lessons 1 and 2 are preserved.

## Get the code

Prerequisites: .NET 10 SDK, Git, VS Code with C# support and Postman. Git tracks local work; GitHub hosts this public repository. Reading/cloning it does not require a GitHub account. A normal clone begins on main, the Module 1 starter.

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git status
git fetch origin
git switch -c my-module-03-lesson-03 origin/module-03-lesson-03-start
```

For an existing checkout, commit or stash unfinished work including untracked files before switching. Use a fresh practice-branch name if that name exists. Open Stockline.Orders.slnx.

| Lesson | Start | Completed comparison |
|---|---|---|
| 1 - Decorator diagnostics | module-03-lesson-01-start | module-03-lesson-01-complete |
| 2 - Observer reactions | module-03-lesson-01-complete | module-03-lesson-02-complete |
| 3 - Factory and eligibility | module-03-lesson-03-start | module-03-lesson-03-complete |
| 4 - One-handler mediator | module-03-lesson-04-start | module-03-lesson-04-complete |


## Baseline and completed behavior

The starting application is completed Lesson 2: standard CAB-105 quantity10 creates a Confirmed order at63.75, one application payment attempt and two local previews. The updated setup guide and Postman requests are supplied; trade/customer validation behavior is implemented during this lesson.

The simple factory chooses NoCustomerDiscount for Standard or TradeCustomerDiscount for Trade; unknown enum categories fail explicitly. The trade policy offers20% only at quantity>=10. The selected customer offer joins the existing best-offer scan: discounts are not stacked. Customer validation precedes order creation and authorization. CustomerCatalog contains local fixture IDs standard(default), trade and inactive, case-sensitive. Omitted/null customerId keeps the original standard behavior. Unknown/inactive IDs return400. These IDs do not authenticate a caller; production identity and pricing benefits must come from authenticated server context.

| Case | Completed result |
|---|---|
| Standard CAB10 | 201 / Confirmed /63.75 |
| Trade CAB9 | 201 /57.38; cable15% wins |
| Trade CAB10 /11 | 201 /60.00 /66.00 |
| Standard / Trade BRG10 | 108.00 /96.00 |
| Unknown / inactive customer | 400 before order/payment |

The factory centralizes selection, the policy checks eligibility, and OrderPricing chooses the highest eligible rate with the original tie and rounding rules. A selected trade policy can return no offer. An inline branch remains reasonable for trivial cases; adding a category still requires reviewing the switch and tests.

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

Import postman/Stockline Orders.postman_collection.json. Set baseUrl=http://127.0.0.1:7376. Open Module03 / Lesson03. Use the default-standard request for the baseline; additional completed expectations apply after your implementation. Each fresh POST saves its returned order ID and operationKey as collection variables. Read the saved order and previews by order ID, and payment attempts by operationKey in the same API process. Restarting clears local stores. Receipt total and payment amount match saved total; fulfillment shows product and quantity. These are local previews, not email or shipments. Development read endpoints are hidden in Production.

## Checks after implementation

Before edits, the starting checkpoint passes137 checks. The completed checkpoint passes161. New constructor dependencies require the matching test fixtures; after writing your own application changes, fetch and restore only the supplied checks, preserving your edited src files:

```powershell
git fetch origin
git restore --source origin/module-03-lesson-03-complete -- tests/Stockline.Lesson04.Checks
dotnet run --project tests/Stockline.Lesson04.Checks -- --factory-only
dotnet run --project tests/Stockline.Lesson04.Checks
```

Only restore that checks folder, not the full completed application. The historical project name Stockline.Lesson04.Checks is retained so earlier commands stay compatible. The complete source is for comparison after your attempt. The video opens actual retained check output in the editor; this is not a live terminal recording.
