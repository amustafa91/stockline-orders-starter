# Stockline Orders — Module 3 Lesson 02: One Order, Two Local Reactions

Starting checkpoint: [`module-03-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-01-complete). Completed comparison: [`module-03-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-02-complete).

The preceding lesson's learner scaffold, [`module-03-lesson-01-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-01-start), contains full working L4 plus three inactive Diagnostics support files and 62 baseline checks. L01 completed code has 37 diagnostic / 104 cumulative checks. Its decorator records the exact original decimal amount, including 63.75 in the normal CAB checkout.

## Get the code and choose your checkpoint

Prerequisites: .NET 10 SDK, Git, a C# editor and Postman. Git tracks your local work; GitHub hosts the public repository. You do not need a GitHub account to clone it.

Clone once, then work from the repository root:

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
```

A normal clone begins on `main`, the Module 1 starter. Module 3 continues the completed Module 2 application; choose its lesson checkpoint explicitly.

| Lesson | Start from | Compare after your attempt |
|---|---|---|
| M03L01 — Attempt Diagnostics | [`module-03-lesson-01-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-01-start) | [`module-03-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-01-complete) |
| M03L02 — One Order, Two Local Reactions | [`module-03-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-01-complete) | [`module-03-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-03-lesson-02-complete) |

For an existing clone, save your files and commit or stash unfinished work, including untracked files, before changing branches. Check `git status`, fetch the checkpoint refs, then create your own practice branch from the remote lesson start:

```powershell
git status
git fetch origin
git switch -c my-module-03-lesson-02 origin/module-03-lesson-01-complete
```

Choose a fresh practice-branch name if that name already exists. Keep your own edited application checked out while testing it; use the completed branch for comparison after your attempt. The unmodified starting checkpoint and the completed checkpoint have different expected checks.

## Why add these reactions?

After confirmation, the local teaching application can show a receipt preview and a fulfillment-preview projection. These are optional projections of the saved order; they do not send email, reserve inventory or create shipments.

Start with two concrete calls after `OrderStore.Complete`. That working direct-call intermediate is retained as the separately supplied `source/direct` comparison snapshot. It actually builds and has 9 focused / 113 cumulative checks plus real local HTTP receipts. Acquire this comparison snapshot from the lesson assets; it is separate from a repository clone. The same Lesson 02 Postman requests work against its API and the completed observer implementation.

Direct calls are reasonable for two stable reactions. The observer comparison moves knowledge of the subscriber list to composition when adding/removing local reactions independently is useful. It adds one domain-specific callback interface and one concrete publisher; there is no generic bus, mediator, async dispatch framework or background worker.

## Exact order and failure policy

1. Validate the request and calculate the server-owned total.
2. Start the order and invoke the existing decorated payment gateway once.
3. Save the original payment decision with `OrderStore.Complete`.
4. Only for a saved `Confirmed` order, construct immutable `OrderConfirmed` data from its captured values.
5. Invoke each registered callback synchronously and sequentially: receipt, then fulfillment.
6. Catch an individual subscriber exception, retain its type name as best-effort preview-failure visibility, and continue to the next subscriber. Return the original saved order.

A failed receipt preview therefore leaves the confirmed order and 201 response intact, while the fulfillment preview still runs. A failed second subscriber does not undo the first. A callback throwing `OperationCanceledException` is a callback failure, not a new promise to cancel or roll back checkout. The callback contract has no cancellation token. Original gateway exceptions and caller cancellation still propagate before confirmation and cause no reactions; a cancellation race cannot replace an already-returned approval.

Declined and Unknown decisions retain their existing 409/Declined and 202/PendingReconciliation behavior, without invoking either confirmed-order subscriber. No retry, deduplication, financial audit or reconciliation mechanism is added. Unknown still means indeterminate.

A successful invocation attempts each registered subscriber once. Publishing again calls it again. A process failure after confirmation can lose all or some callbacks; restart loses all previews and orders. No durable publication record or eventual-delivery guarantee exists. Callbacks must remain fast and local, because they run on the request path.

## Files and lifetimes

- `Application/OrderConfirmed.cs`: immutable saved-order data.
- `Application/IOrderConfirmedReaction.cs` and `LocalOrderConfirmedPublisher.cs`: explicit sequential callback contract and failure policy.
- `Previews/OrderPreviewReactions.cs`: receipt and fulfillment implementations.
- `Previews/LocalOrderPreviewStore.cs`: process-local projections, retaining at most 100 orders; at most eight distinct failure type names of 128 characters per order. Eviction uses first-observed order, not completion time. Read arrays are detached.
- `Application/PlaceOrder.cs`: save completion first, publish only for Confirmed.
- `CheckoutRegistration.cs`: singleton publisher, store and stateless subscribers; original scoped workflow, decorator and adapter remain scoped. No singleton captures request-scoped dependencies.
- `Controllers/OrderPreviewsController.cs`: Development-only read view.

Paths above are under `src/Stockline.Api`. The direct-call intermediate has concrete receipt/fulfillment dependencies instead of the observer interface and publisher.

## Run checks and API

Run from the repository root after implementing the lesson on your practice branch:

```powershell
dotnet build Stockline.Orders.slnx
dotnet run --project tests/Stockline.Lesson04.Checks -- --previews-only
dotnet run --project tests/Stockline.Lesson04.Checks
dotnet run --project src/Stockline.Api
```

Build along before expecting the new checks. Completed observer counts: **30 preview checks**, **137 cumulative checks**; the existing 37 diagnostic checks remain available with `--diagnostics-only`. The cumulative check project deliberately retains `tests/Stockline.Lesson04.Checks` for historical compatibility with earlier checkpoints and lesson commands; it now verifies the evolving course application. Renaming it across the course is outside this lesson. Run API and checks in separate terminals. The normal profile uses Development and `http://localhost:7375`.

## Postman and partial-failure rehearsal

Reimport the existing `postman/Stockline Orders.postman_collection.json`; do not create a duplicate collection. Open Module 03 -> Lesson 02. Run one scenario folder at a time. The historical lesson examples remain checkpoint-specific.

With Approve / SUCCESS and no failure override, POST CAB-105 quantity 10, then read the saved order, previews and payment attempt in the SAME running process. Expected: 201/Confirmed, saved total 63.75, one receipt preview for 63.75, one fulfillment preview for CAB-105 quantity 10 and one diagnostic attempt for 63.75.

```http
GET /orders/{returned-order-id}/previews
GET /diagnostics/payment-attempts?operationKey={returned-operation-key}
```

The preview view returns 404 for an absent/evicted preview, including declined/unknown orders, and outside Development. GET never dispatches callbacks. Failure names indicate observed local callback failure, not a durable audit; old names can remain until eviction if a notification is manually replayed.

For the deliberate partial-failure fixture, stop the API, then run:

```powershell
$env:OrderPreviews__FailReceipt = 'true'
$env:PaymentProvider__Scenario = 'Approve'
dotnet run --project src/Stockline.Api
```

The failure flag is honored only in Development and throws BEFORE generating the receipt preview, AFTER confirmation has already been saved. Send a fresh POST: it still returns 201 and the order GET returns Confirmed/63.75. The preview view shows `receipt: null`, a populated fulfillment projection and `failedReactions: ["ReceiptPreviewReaction"]`. Payment diagnostics still contain one original attempt. Do not retry payment to repair a preview.

Stop the API before changing scenarios. Set the failure flag to `false`; repeat with `PaymentProvider__Scenario` equal to `Decline`, then `AuthorizeThenDisconnect`. These keep 409 and 202 respectively; order GET works but preview GET returns 404. Restore `Approve` and remove the failure override afterward. These are local handler scenarios, not real provider calls.

To verify reads are hidden outside Development, set BOTH `ASPNETCORE_ENVIRONMENT` and `DOTNET_ENVIRONMENT` to `Production`, set the loopback `ASPNETCORE_URLS`, and use `dotnet run --no-launch-profile --project src/Stockline.Api`. The launch profile otherwise selects Development. Remove those overrides afterward. No authenticated production support endpoint is claimed.

## Teaching edit sequence

1. Start with the verified L01 checkout and its recorded original payment amount. Explain the two optional previews and their independence from authoritative confirmation.
2. Implement the concrete preview types and direct receipt/fulfillment callers after saved confirmation. Run the direct-call comparison and Postman proof, including no reactions for decline/unknown.
3. Deliberately fail the receipt preview after confirmation. Keep the order confirmed, continue fulfillment and inspect partial results. State the failure policy before abstracting it.
4. Introduce the single local callback interface and sequential publisher. Move subscriber registration into composition, replacing the concrete preview dependencies in PlaceOrder. Compare actual direct source with completed observer source.
5. Run the same HTTP cases and focused checks. Inject failures at both subscriber positions, cancellation from a subscriber and original gateway failure/cancellation. Demonstrate repeated publication has no deduplication.
6. Explain the added navigation/indirection cost and local-loss window. Keep durable delivery for the later messaging module.

Exercise: in an isolated test, add a third local subscriber, prove its invocation order and inject failure in the middle subscriber. The later subscriber must still run; the saved order must remain unchanged. Do not add retries, queues or network effects.
