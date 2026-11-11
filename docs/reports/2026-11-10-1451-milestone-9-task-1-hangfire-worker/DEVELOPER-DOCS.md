# Milestone 9 Task 1 — Hangfire server with an empty `WorkerTickJob`

## Summary

Milestone 9 Task 1 (`tasks/todo-m9.md`) replaced the template Worker loop with a Hangfire server, which is the riskiest part of the milestone.
- The Worker now stores Hangfire's data in SQL Server, in the `HangFire` schema of the `Klasly` database.
- On startup it registers one recurring job, `WorkerTickJob`, on the cron from `Worker:Cron`.
- The job only logs a line for now. Tasks 3–4 add "close ended sessions" and "mark Absent".

No business logic and no Klasly table changed.

## Projects touched

CONSOLE WORKER (and MSSQL: Hangfire created its own `HangFire` schema).

## Files changed

| Path | Change | Why |
|---|---|---|
| `src/Klasly.Worker/Klasly.Worker.csproj` | modified | + `Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore` 1.8.25; removed the empty `Jobs\` folder entry |
| `src/Klasly.Worker/Program.cs` | modified | Infrastructure + Hangfire storage/server; registers the recurring job; clear errors for a missing connection string or cron |
| `src/Klasly.Worker/Jobs/WorkerTickJob.cs` | added | The Worker's one recurring job; no overlap, no Hangfire retries |
| `src/Klasly.Worker/Worker.cs` | deleted | Template loop that logged every second |
| `src/Klasly.Worker/appsettings.json` | modified | `Worker:Cron = */5 * * * *`; EF Core logs at Warning |
| `src/Klasly.Worker/appsettings.Development.json` | modified | LocalDB connection string `Klasly` (Windows auth, no password), same as the API |

## Key code

```csharp
// Program.cs
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString));
builder.Services.AddHangfireServer();
var host = builder.Build();
host.Services.GetRequiredService<IRecurringJobManager>()
    .AddOrUpdate<WorkerTickJob>("worker-tick", job => job.RunAsync(CancellationToken.None), cron);
```

```csharp
// Jobs/WorkerTickJob.cs
[DisableConcurrentExecution(timeoutInSeconds: 600)] // two ticks never overlap, even across Worker processes
[AutomaticRetry(Attempts = 0)]                      // the outbox (Milestone 12) does its own retries
public class WorkerTickJob(ILogger<WorkerTickJob> logger) { public Task RunAsync(CancellationToken ct) … }
```

- **Shutdown token:** Hangfire swaps `CancellationToken.None` in the job expression for the server's shutdown token at run time.
- **Job instances:** Hangfire.NetCore creates the job in its own DI scope per run, so later tasks get a fresh `KlaslyDbContext` each tick.
- **Cron:** cron times are UTC (Hangfire's default).
- **Registration:** `AddOrUpdate` runs on every start, so a changed `Worker:Cron` applies after a restart.

## Packages

| Package | Version | Why |
|---|---|---|
| Hangfire.Core | 1.8.25 | Recurring jobs, `[DisableConcurrentExecution]`, `[AutomaticRetry]` |
| Hangfire.SqlServer | 1.8.25 | Job storage in the `Klasly` database (uses `Microsoft.Data.SqlClient` 6.1.7, already there through EF Core) |
| Hangfire.NetCore | 1.8.25 | `AddHangfire` / `AddHangfireServer` for the generic host |

`dotnet list src/Klasly.Worker package --include-transitive`:
- every `Microsoft.Extensions.*` and `Microsoft.EntityFrameworkCore.*` package is **10.0.14**;
- Hangfire's old minimum versions (`*.Abstractions` ≥ 2.0) resolved to 10.0.14;
- the only other old-looking entries (`Microsoft.Bcl.AsyncInterfaces` 8.0.0, `Newtonsoft.Json` 13.0.4) are already in the API through EF Core Design, so Hangfire did not add them.

## Database changes

No EF Core migration and no Klasly table changed. Hangfire created **11 tables in the `HangFire` schema** on first run. They are not modelled in EF Core and not counted in the 17 tables (CLAUDE.md §3.1).

## How it was verified

- `dotnet build src/Klasly.Worker` → Build succeeded, 0 warnings. `dotnet build src/Klasly.Web` → succeeded.
- `dotnet build Klasly.slnx` failed **only** on copying `Klasly.Core.dll` / `Klasly.Infrastructure.dll` into `Klasly.API\bin`. The file was locked by a running `Klasly.API` process (PID 23596). There were no compile errors. Re-run it once the API is stopped.
- Ran the Worker for 150 s with `Worker__Cron = "* * * * *"`:
  - log: "Hangfire SQL objects installed", "all the dispatchers started", then `Worker tick at 2026-11-10 06:50:02Z` and `06:51:01Z`;
  - `HangFire.Job`: 2 jobs `Succeeded`; `HangFire.Set` has `recurring-jobs`.
- Row counts of all 17 Klasly tables + `__EFMigrationsHistory` before and after the run: identical (`diff` empty).

## Known limits / TODO

- **Cron left in the database:** the Hangfire tables still hold the last cron used (`* * * * *` from the test). The next normal start re-registers `*/5 * * * *`.
- **Old open sessions:** the database has **11 `Open` sessions** from earlier testing. Once Task 3 lands, the first tick closes every one past its end time, and Task 4 then marks their non-scanners Absent. That's expected, but your old test sessions will change.
- **Not checked:** a missing connection string fails at startup with "Connection string 'Klasly' is missing." That's by design but wasn't tested. A Worker started without `DOTNET_ENVIRONMENT=Development` (outside `dotnet run`'s launch profile) has no connection string.
