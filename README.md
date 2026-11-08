# Stockline Orders — completed Lesson 2

This checkpoint continues from lesson-01-complete and adds product choice. It uses controllers in one project. Prices belong to the server; the request supplies SKU and quantity (1–100).

| SKU | Unit price |
|---|---:|
| BRG-204 | USD 12.00 |
| CAB-105 | USD 7.50 |

From the repository root run:
```powershell
dotnet run --project src/Stockline.Api
```
Use http://localhost:7375. Import postman/Stockline Orders.postman_collection.json, then use Module 01 / Lesson 02. The required SKU intentionally evolves the request contract; earlier examples in this collection include BRG-204. Extra JSON price/total properties are ignored and cannot set the authoritative price. Missing/unknown SKU or quantity outside 1–100 is rejected before order/payment side effects.

Run the focused checks:
```powershell
dotnet run --project tests/Stockline.Lesson02.Checks
```
Orders are in memory and disappear on restart. Payment is a local simulation. Captured order totals are historical values, not recalculated from later catalog data. There is no bulk discount yet: that is Lesson 3.

Exercise: add one product, verify the receipt and authorized amount, and show an unknown SKU never reaches payment.


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
git switch lesson-01-complete
code .
```

Implement Lesson 2 in your own branch. To inspect this completed checkpoint, save your work and switch to `lesson-02-complete`.

Import the collection from the currently checked-out branch: **Stockline Orders → Module 01 - Clean Code Foundations → Lesson 02**. Run only that lesson folder against this checkpoint; different folders represent different code versions. Reimport the same collection ID when updating it, rather than keeping duplicate collections. Send a successful POST before Get Last Order after every API restart.
