# How to use — the Klasly Worker (Hangfire), Milestone 9 Task 1

The Worker is a console program that runs in the background. Every 5 minutes it runs one job called **Worker tick**. Right now the tick only writes a log line. Later tasks make it close ended sessions and mark absent students.

## Prerequisites

- SQL Server LocalDB with the `Klasly` database (created by the EF Core migrations in Milestone 2).
- .NET 10 SDK.
- Nothing else: the Worker doesn't need the API, the website or the emulator.

## Step by step

1. Open a terminal in the solution folder (`Klasly/`).
2. Run:
   ```bash
   dotnet run --project src/Klasly.Worker
   ```
3. Wait for `Application started. Press Ctrl+C to shut down.` The first run also prints `Hangfire SQL objects installed`, because it creates Hangfire's tables.
4. Every 5 minutes (at :00, :05, :10 … UTC), a line `Worker tick at …` appears.
5. Press **Ctrl+C** to stop.

### Faster ticks for testing

To tick every minute, set the cron for this terminal only (PowerShell):
```powershell
$env:Worker__Cron = "* * * * *"; dotnet run --project src/Klasly.Worker
```
Close the terminal (or `Remove-Item Env:Worker__Cron`) to go back to every 5 minutes.

## Example

```
info: Hangfire.SqlServer.SqlServerObjectsInstaller[0]
      Hangfire SQL objects installed.
info: Hangfire.BackgroundJobServer[0]
      Starting Hangfire Server using job storage: 'SQL Server: (localdb)\MSSQLLocalDB@Klasly'
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Klasly.Worker.Jobs.WorkerTickJob[0]
      Worker tick at 2026-11-10 06:50:02Z
```

In SQL Server Object Explorer, the `Klasly` database now also has tables named `HangFire.*`. They belong to Hangfire. Don't edit them.

## Troubleshooting

| Problem | Fix |
|---|---|
| `Connection string 'Klasly' is missing.` | Run with `dotnet run` (it uses the Development settings), or set `DOTNET_ENVIRONMENT=Development`. |
| `Cannot open database "Klasly"` / a LocalDB error | Start LocalDB (`sqllocaldb start MSSQLLocalDB`) and make sure the database exists: `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`. |
| No "Worker tick" lines after 5 minutes | Ticks happen on the 5-minute marks of the clock, not 5 minutes after start. Use the 1-minute cron above to check quickly. |
