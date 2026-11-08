# Stockline Orders — Lesson 3 completed checkpoint

Module 1: Clean Code Foundations. Add wholesale pricing without changing storage.

Start from your completed Lesson 2 solution. Use .NET 10 SDK, Git, VS Code with C# support and Postman. This is one controller-based project; there is no database or minimal API.

```powershell
dotnet run --project src/Stockline.Api
dotnet run --project tests/Stockline.Lesson03.Checks
```

API: http://localhost:7375. Import the single Postman collection and open Module 01 / Lesson 03. BRG-204 costs USD 12 per unit; CAB-105 costs USD 7.50. Quantities 10–100 receive 10% off the entire subtotal. Round the final total once to two decimals using AwayFromZero. Nine and ten BRG units both cost USD 108: this follows the chosen rule, not a coding bug. Orders snapshot the computed amount and payment uses that amount.

OrderPricing is a concrete pure calculator. OrderStore receives the computed total and stores it. The controller coordinates these calls for now. No pricing policy interface is needed for one rule.

Orders remain in memory and disappear on restart. Payment is simulated. Cancellation reaches payment, but this does not make the workflow atomic: an interrupted payment can leave a Pending order. Existing decline/unknown outcomes remain meaningful; reconciliation comes later.

Exercise: Explain and test quantities 9, 10 and 11 plus a rounding midpoint. Change a pricing fixture and verify historical receipts remain unchanged.


## Course checkpoints

Work through the videos in order. Keep your own implementation before switching to comparison code.

| Video | Start from | Completed checkpoint |
|---|---|---|
| Lesson 1 — Isolate the Payment Provider | `main` | [`lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/lesson-01-complete) |
| Lesson 2 — Who Owns the Price? | `lesson-01-complete` | [`lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/lesson-02-complete) |
| Lesson 3 — Wholesale Pricing | `lesson-02-complete` | [`lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/lesson-03-complete) |
| Lesson 4 — Give Checkout a Home | `lesson-03-complete` | [`lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/lesson-04-complete) |

`main` remains the original Lesson 1 starter. The completed Lesson 2 now matches **Who Owns the Price?**, replacing the retired 199-cent naming exercise. No generated video, voice recording, production tool or personal file is included in these checkpoints.

## Clone and choose the checkpoint

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git switch lesson-02-complete
code .
```

Implement Lesson 3 in your own branch. To inspect this completed checkpoint, save your work and switch to `lesson-03-complete`.

Import the collection from the currently checked-out branch: **Stockline Orders → Module 01 - Clean Code Foundations → Lesson 03**. Run only that lesson folder against this checkpoint; different folders represent different code versions. Reimport the same collection ID when updating it, rather than keeping duplicate collections. Send a successful POST before Get Last Order after every API restart.
