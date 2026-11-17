# Stockline Orders — Module 3 Lesson 01: Attempt Diagnostics

Start: Module 2 Lesson 4 completed code, pinned here at `f80c500e194e4c01898992155525aaa6e88f0ead`.
Local completed branch: `module-03-lesson-01-complete`. This completed checkpoint is local; it has not been pushed or published.

Prerequisites: .NET 10 SDK, Git, a C# editor and Postman. Keep the existing one-project application and local payment simulator.

## The change

Support needs to inspect what each application authorization invocation returned, including indeterminate outcomes. The call chain is:

`PlaceOrder -> DiagnosticPaymentGateway -> PaymentAdapter -> SimulatedProvider`

The decorator forwards the original operation key, captured amount and cancellation token exactly once. It returns the original decision or rethrows the original exception. Cancellation is forwarded, not used as a reason to invent a result or roll back provider effects. Even an already-canceled token reaches the inner gateway; if that gateway returns a result, the decorator preserves it.

The adapter still translates provider replies and converts transport failure to `Unknown`. A simulated lost reply therefore records `Returned / Unknown`, not a decorator fault. Existing HTTP mapping is unchanged: approval 201/Confirmed, decline 409/Declined, unknown 202/PendingReconciliation. CAB-105 quantity 10 still captures and authorizes 63.75; the diagnostic observation records that same original decimal amount. This observes the gateway input, not independent proof of an external charge.

## Build and checks

Save unfinished edits before changing branches. Start your own practice branch from `f80c500`; compare against the local completed branch only after implementing your change. Historical Module 2 checkpoints are listed below. No new remote branch is assumed to exist.

```powershell
git status
git switch -c my-module-03-lesson-01 f80c500
```

For the completed implementation, run from the repository root:

```powershell
dotnet build Stockline.Orders.slnx
dotnet run --project tests/Stockline.Lesson04.Checks -- --diagnostics-only
dotnet run --project tests/Stockline.Lesson04.Checks
dotnet run --project src/Stockline.Api
```

Expected completed counts: **37 diagnostic checks**, **104 cumulative checks**. The cumulative runner retains its historical folder name. The solution now includes that runner, so a solution build also compiles the current verification code. The starting L4 checkpoint has 62 cumulative checks.

The first three commands are verification; the final command starts the API. Keep the check runner separate from API startup. `dotnet run` does not watch edits: stop and restart after saving code changes. Use the same API process between each POST, order GET and diagnostic GET.

## Inspect in Postman

Reimport the existing `postman/Stockline Orders.postman_collection.json`; retain one collection. Open **Module 03 -> Lesson 01**. The historical Module 1/2 folders retain their checkpoint-specific examples.

The normal launch profile uses Development and `http://localhost:7375`. Send CAB-105 quantity 10 in the approval folder, then read the returned order and its authorization attempt. The requests save `m03OrderId` and `m03OperationKey` for you.

```http
GET /diagnostics/payment-attempts
GET /diagnostics/payment-attempts?operationKey=authorize:ORD-1048
```

The operation key above is illustrative: use the value returned by your own POST. Reads return a JSON array in recording order. No matches return `[]`; a filter longer than 128 characters returns 400. Reading never calls the gateway or clears the sink. Run the invalid-input folder's three requests in order while no other client is creating orders: retained attempt IDs must stay unchanged after a rejected SKU.

The decline and lost-reply folders are separate manual scenarios, not one automatic collection run. Stop the API before each environment change:

```powershell
$env:PaymentProvider__Scenario = 'Decline'
dotnet run --project src/Stockline.Api
```

After stopping it, use `AuthorizeThenDisconnect` for the lost-reply folder. Restore `Approve` afterward. The existing `PaymentProvider:ApprovedReply` is `SUCCESS`; the handler remains entirely in-process. Restart clears orders and diagnostics, so POST again before reading the returned identifiers. No new provider, scenario-control endpoint or retry is introduced.

The read controller returns 404 outside Development. To rehearse that boundary without the launch profile overriding your environment, stop the API and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
$env:DOTNET_ENVIRONMENT = 'Production'
$env:ASPNETCORE_URLS = 'http://localhost:7375'
dotnet run --no-launch-profile --project src/Stockline.Api
```

Afterward, stop it and remove those three environment overrides before returning to the ordinary launch command. This unauthenticated local teaching view is not a production support interface.

## Retention and failure semantics

`LocalPaymentAttemptSink` keeps at most **100 observations**, evicting the oldest recorded entry. Operation keys and exception-type strings are limited to 128 characters in retained records; original gateway arguments are unchanged. A lock protects recording, eviction and detached snapshot reads. Recording order can differ from invocation or completion-timestamp order under concurrency.

Each observation has a fresh attempt ID, operation key, original decimal amount, UTC completion timestamp, termination (`Returned`, `Canceled`, `Faulted`), optional decision and optional exception type. Reusing an operation key does not deduplicate invocations. No exception messages, stack traces, provider bodies, payment credentials or HTTP timing are retained.

This is non-durable process-local diagnostics. Sink failure, eviction or process termination can lose observations. A cancellation observation establishes that cancellation was thrown, not that an authorization was undone. These entries are not a financial audit trail, provider transaction history, retry policy or exactly-once guarantee. Built-in HTTP tracing remains responsible for transport diagnostics.

## Teaching edit sequence

1. Start from the working L4 checkout: POST/GET 63.75 and run its existing checks. Explain the missing application-attempt view and compare adding diagnostics inside the adapter.
2. Add the small immutable observation and sink contract. Implement bounded local retention, showing why a singleton is needed across POST and diagnostic GET requests.
3. Write focused forwarding/result/error/cancellation checks; add the decorator around an inner `IPaymentGateway`. Keep one call and rethrow original failures.
4. Show a failing sink and prove that best-effort recording cannot mask the inner outcome. Explain why recording exceptions are isolated from the business call.
5. Wire the concrete scoped adapter inside the scoped decorator. Update the composition assertion intentionally; retain lifetime validation and gateway substitution checks. Run the wrapped gateway contract.
6. Add the Development-only read controller and Postman requests. Demonstrate Approved, Declined and Unknown with unchanged checkout responses, then invalid input adding no attempt.
7. Run focused and cumulative checks. Explain eviction/restart loss, and leave the adapter, workflow and result mapping unchanged.

Exercise: use a capacity-three sink fixture, make four distinguishable attempts and explain the retained order. Then demonstrate that two invocations with the same operation key remain distinct observations. Keep local order-confirmed reactions for M03L02; no subscribers or event dispatcher belong in this lesson.

## Module 2 checkpoints

| Video | Start from | Compare after finishing |
|---|---|---|
| 1. What Happens When Discounts Overlap | [`module-02-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-start) | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) |
| 2. When Does a Pricing Policy Earn Its Interface | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) |
| 3. A Substitute Must Keep the Promise | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) |
| 4. Dependency Direction Is More Than DI Registration | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) |
| Recap â€” What Can Change | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) | Same checkpoint; use a practice branch for the exercise |

`main` starts Module 1. For Module 2, finish Module 1 first, then use `module-02-start`. Each completed branch is the next lesson's starting code. The recap uses the Lesson 4 completion. Use these public GitHub branches as the course checkpoints; the optional exercises are yours to implement.
