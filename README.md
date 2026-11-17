# Stockline Orders — Module 3 Lesson 01 starting scaffold

This is the complete working Module 2 Lesson 4 application from `f80c500e194e4c01898992155525aaa6e88f0ead`, plus three supplied support files. Expected baseline: **62 cumulative checks**.

Local starting branch: `module-03-lesson-01-start`. Local completed comparison: `module-03-lesson-01-complete`. These branches are prepared locally; no new remote branch or publication is assumed.

## Acquire the support scaffold before the lesson

Use this starting checkpoint rather than bare `f80c500`. Preserve your own work before switching branches, then create a practice branch:

```powershell
git status
git switch module-03-lesson-01-start
git switch -c my-module-03-lesson-01
```

The supplied files, under `src/Stockline.Api/Diagnostics`, are:

- `PaymentAttempt.cs`: an immutable observation with the original decimal Amount, operation key, completion time, termination and optional decision/exception type.
- `IPaymentAttemptSink.cs`: the recording contract.
- `LocalPaymentAttemptSink.cs`: bounded, process-local retention and detached snapshots; at most 100 entries and 128-character retained strings.

Read these support types when the lesson introduces them. They are supplied scaffold, not edits demonstrated or implemented in the film. They are not registered or used by the starting application. Restart and eviction lose observations; this is not a financial audit or durable history.

## Verify and run the unchanged starting application

Prerequisites: .NET 10 SDK, Git, a C# editor and Postman. From the repository root:

```powershell
dotnet run --project tests/Stockline.Lesson04.Checks
dotnet run --project src/Stockline.Api
```

Run the commands in separate terminals. The first reports **62 checks**. The normal Development launch profile serves `http://localhost:7375`. Payment remains an in-process simulator configured as Approve / SUCCESS. Import the existing Postman collection and use the Module 2 Lesson 4 checkout: CAB-105 quantity 10 returns 201 and 63.75; GET its returned identifier in the same API process. Restart clears orders.

The historical solution contains only the API project, so run the check project explicitly. No diagnostic read endpoint exists in this starting checkpoint.

## Edits the learner makes on screen

Implement `DiagnosticPaymentGateway`, wire the existing scoped adapter inside it in `CheckoutRegistration`, and add the Development-only `PaymentAttemptsController`. Forward the original key, decimal amount and token once; preserve returned decisions, exceptions and cancellation; isolate sink failure. Then verify the amount in the returned observation and repeat approval, decline and unknown scenarios in Postman.

The completed checkpoint adds the focused and cumulative diagnostics checks, updated composition assertions, solution test-project entry and Module 3 Postman requests. Expected completed counts: **37 focused**, **104 cumulative**. Those counts are not claims about the unmodified scaffold. Compare after your implementation; keep your own application checked out when testing it. M03L02 reactions are outside this lesson.
