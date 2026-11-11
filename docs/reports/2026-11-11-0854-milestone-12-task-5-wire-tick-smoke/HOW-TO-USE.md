# How to use — Milestone 12 Task 5 (Parent alert emails in the Worker)

Every 5 minutes, the Worker now also writes absence and at-risk emails for parents and tries to send them through Brevo. Until the team sets up Brevo (Task 6), each email is marked **Failed — "Brevo is not configured"**, and the Worker keeps doing everything else.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- .NET 10 SDK.
- For real emails (Task 6 only): a Brevo API key in user-secrets, and a verified sender address.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Start the Worker. Ticking every minute instead of every 5 makes testing faster:
   ```
   dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"
   ```
3. Within a minute, the log shows one line per step:
   ```
   Tick …: closed 0, cancelled 0, finalized 0 sessions
   Tick …: risk for 10 enrollments, 0 new snapshots, 10 at-risk
   Tick …: queued 98 absence and 10 at-risk emails
   warn: Brevo is not configured (Brevo:ApiKey / Brevo:SenderEmail): due emails are marked Failed
   Tick …: emails sent 0, retrying 0, failed 50
   ```
4. Look at the outbox:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -E -Q "SELECT TOP 20 NotificationId, Type, Status, Attempts, LastError, DedupKey FROM Notifications WHERE Channel = 'Email' ORDER BY NotificationId DESC"
   ```
5. Stop the Worker with `Ctrl+C`.

## Example: what each part of an alert means
| Column | Example | Meaning |
| --- | --- | --- |
| `Type` | `Absence` / `AtRisk` | which alert |
| `Status` | `Pending` → `Accepted` or `Failed` | waiting, sent, or given up |
| `Attempts` | `0` | how many times Brevo was called |
| `NextAttemptAt` | `2026-11-11 09:10` | when a busy-Brevo retry is due (UTC) |
| `DedupKey` | `absent:1019:6:3` | session 1019, student 6, guardian 3; this exact alert is never queued again |

## Troubleshooting
- **"Brevo is not configured" on every tick**: expected until Task 6. Set the key with `dotnet user-secrets set "Brevo:ApiKey" "<key>" --project src/Klasly.Worker`, and set `Brevo:SenderEmail` to a sender verified in Brevo.
- **An alert you expected was never sent, even after adding the key**: alerts queued while Brevo wasn't configured stay Failed on purpose, so a backlog can't flood parents the day the key is added. Test with a new absence instead.
- **"Brevo rejected the API key (401 …)" in the log**: the key is wrong or revoked. The emails stay Pending and go out on the first tick after you fix the key.
