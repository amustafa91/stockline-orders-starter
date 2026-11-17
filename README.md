# Stockline Orders — Module 3 Lesson 01 starting scaffold

This is the complete working Module 2 Lesson 4 application from `f80c500e194e4c01898992155525aaa6e88f0ead`, plus three supplied support files. Expected baseline: **62 cumulative checks**.

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
git switch -c my-module-03-lesson-01 origin/module-03-lesson-01-start
```

Choose a fresh practice-branch name if that name already exists. Keep your own edited application checked out while testing it; use the completed branch for comparison after your attempt. The unmodified starting checkpoint and the completed checkpoint have different expected checks.

## Supplied support scaffold

Use this starting checkpoint rather than bare `f80c500`. It includes the full working L4 application and three inactive support types; the decorator, DI wiring and controller remain learner edits.

The supplied files, under `src/Stockline.Api/Diagnostics`, are:

- `PaymentAttempt.cs`: an immutable observation with the original decimal Amount, operation key, completion time, termination and optional decision/exception type.
- `IPaymentAttemptSink.cs`: the recording contract.
- `LocalPaymentAttemptSink.cs`: bounded, process-local retention and detached snapshots; at most 100 entries and 128-character retained strings.

Read these support types when the lesson introduces them. They are supplied scaffold, not edits demonstrated or implemented in the film. They are not registered or used by the starting application. Restart and eviction lose observations; this is not a financial audit or durable history.

## Verify and run the unchanged starting application

From the repository root:

```powershell
dotnet run --project tests/Stockline.Lesson04.Checks
dotnet run --project src/Stockline.Api
```

Run the commands in separate terminals. The first reports **62 checks**. The normal Development launch profile serves `http://localhost:7375`. Payment remains an in-process simulator configured as Approve / SUCCESS. Import the existing Postman collection and use the Module 2 Lesson 4 checkout: CAB-105 quantity 10 returns 201 and 63.75; GET its returned identifier in the same API process. Restart clears orders.

The historical solution contains only the API project, so run the check project explicitly. No diagnostic read endpoint exists in this starting checkpoint.

## Edits the learner makes on screen

Implement `DiagnosticPaymentGateway`, wire the existing scoped adapter inside it in `CheckoutRegistration`, and add the Development-only `PaymentAttemptsController`. Forward the original key, decimal amount and token once; preserve returned decisions, exceptions and cancellation; isolate sink failure. Then verify the amount in the returned observation and repeat approval, decline and unknown scenarios in Postman.

The completed checkpoint adds the focused and cumulative diagnostics checks, updated composition assertions, solution test-project entry and Module 3 Postman requests. Expected completed counts: **37 focused**, **104 cumulative**. Those counts are not claims about the unmodified scaffold. Compare after your implementation; keep your own application checked out when testing it. M03L02 reactions are outside this lesson.
