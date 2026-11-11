# Stockline Orders — Module 2: Starting checkpoint

Prerequisites: .NET 10 SDK, Git, a C# editor and Postman. Git manages your local code history; GitHub hosts the public repository. You do not need a GitHub account to clone it.

## Module 2 checkpoints

| Video | Start from | Compare after finishing |
|---|---|---|
| 1. What Happens When Discounts Overlap | [`module-02-start`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-start) | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) |
| 2. When Does a Pricing Policy Earn Its Interface | [`module-02-lesson-01-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-01-complete) | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) |
| 3. A Substitute Must Keep the Promise | [`module-02-lesson-02-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-02-complete) | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) |
| 4. Dependency Direction Is More Than DI Registration | [`module-02-lesson-03-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-03-complete) | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) |
| Recap — What Can Change | [`module-02-lesson-04-complete`](https://github.com/amustafa91/stockline-orders-starter/tree/module-02-lesson-04-complete) | Same checkpoint; use a practice branch for the exercise |

`main` starts Module 1. For Module 2, finish Module 1 first, then use `module-02-start`. Each completed branch is the next lesson's starting code. The recap uses the Lesson 4 completion. Use these public GitHub branches as the course checkpoints; the optional exercises are yours to implement.


## Build along

Clone once. For an existing clone, save your files and commit or stash unfinished work before switching branches. Run `git status`, then `git fetch origin` to update remote references. Keep application edits on your own practice branch.

```powershell
git clone https://github.com/amustafa91/stockline-orders-starter.git
cd stockline-orders-starter
git fetch origin
git switch module-02-start
git switch -c my-module-02-lesson-01
```

The commands select the lesson's starting code and create your own practice branch. Use the completed branch's GitHub page for comparison after your attempt; keep your practice application checked out when testing your edits.

Run the API and console checks from the repository root in separate terminals:

```powershell
dotnet run --project src/Stockline.Api
dotnet run --project tests/Stockline.Lesson04.Checks
```

API: `http://localhost:7375`. The checks folder retains its Module 1 name; it contains the cumulative checks for the current checkpoint. Expected counts: start 42, Lesson 1 50, Lesson 2 52, Lesson 3 56, Lesson 4 and recap 62.

Import `postman/Stockline Orders.postman_collection.json`. Use the Module 02 folder and the matching lesson. Reimport the same collection instead of creating duplicates. `dotnet run` does not watch code edits. After an edit, save, stop the API with Ctrl+C, rerun the API command above, and wait for listening. Send a fresh POST, then GET its returned order ID while keeping the SAME API process running between those requests. Payment is simulated; this process-local memory store is neither durable nor distributed storage, and restarting clears its orders.

Keep the console check runner separate from API startup: `tests/Stockline.Lesson04.Checks/Program.cs` versus `src/Stockline.Api/Program.cs`.

### Bring in completed verification without replacing your application

First preserve your edits to `tests/Stockline.Lesson04.Checks`: commit them or keep a separate saved copy. The file-only restore below **replaces the working files in that test folder**. Stay on your own practice branch; do not switch to the completed application to test your edits.

```powershell
git status
git fetch origin
git restore --source=origin/module-02-lesson-01-complete -- tests/Stockline.Lesson04.Checks
dotnet run --project tests/Stockline.Lesson04.Checks
```

Review the restored diff and reconcile your saved test edits. This restores only verification files; your application and Postman files stay on your practice branch. The target above is this lesson's completed checkpoint (Lesson 1 completion for the Module 2 starting checkpoint). For another lesson or the recap, select its completed comparison from the map; the recap uses Lesson 4 completion.

The expected counts above describe the unmodified checkpoints. Newly restored checks require your lesson implementation and can fail before it is complete. Run them against your own edited application, not the finished application branch.
