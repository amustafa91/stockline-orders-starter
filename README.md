# Stockline Orders — Module 2: Lesson 02 completed — When Does a Pricing Policy Earn Its Interface

Prerequisites: .NET 10 SDK, Git, a C# editor and Postman. Git manages your local code history; GitHub hosts the public repository. You do not need a GitHub account to clone it.

## Module 2 checkpoints

| Video | Start from | Compare after finishing |
|---|---|---|
| 1. What Happens When Discounts Overlap | [`module-02-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-start) | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) |
| 2. When Does a Pricing Policy Earn Its Interface | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) |
| 3. A Substitute Must Keep the Promise | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) |
| 4. Dependency Direction Is More Than DI Registration | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) |
| Recap — What Can Change | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) | Same checkpoint; use a practice branch for the exercise |

`main` starts Module 1. For Module 2, finish Module 1 first, then use `module-02-start`. Each completed branch is the next lesson's starting code. The recap uses the Lesson 4 completion. These checkpoints match the delivered videos and ZIPs; the optional exercises are yours to implement.


## Build along

Clone once. If you already have a clone, save or commit your own edits before switching branches, then run `git fetch origin`.

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git switch module-02-lesson-01-complete
git switch -c my-module-02-lesson-02
```

The command selects this lesson's starting code. This page's completed branch is for comparison after your attempt. The delivered ZIPs remain an equivalent option, as shown in the videos.

Run the API and console checks from the repository root in separate terminals:

```powershell
dotnet run --project src/Stockline.Api
dotnet run --project tests/Stockline.Lesson04.Checks
```

API: `http://localhost:7375`. The checks folder retains its Module 1 name; it contains the cumulative checks for the current checkpoint. Expected counts: start 42, Lesson 1 50, Lesson 2 52, Lesson 3 56, Lesson 4 and recap 62.

Import `postman/Stockline Orders.postman_collection.json`. Use the Module 02 folder and the matching lesson. Reimport the same collection instead of creating duplicates. After restarting the API, POST an order before fetching it. Payment is simulated; orders are in memory and disappear on restart.

Keep the console check runner separate from API startup: `tests/Stockline.Lesson04.Checks/Program.cs` versus `src/Stockline.Api/Program.cs`. To test your edits with the next checkpoint's new assertions, copy the check files from its completed branch or ZIP, then run the console checks.
