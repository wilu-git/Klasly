# How to use — Milestone 12 Task 2 (Email sender)

There is still nothing to click. This task adds the part of the Worker that goes through the waiting emails and sends them through Brevo. It joins the Worker's 5-minute tick in Task 5. For now you check it with its tests, which use a fake Brevo and a test database.

## Prerequisites
- .NET 10 SDK.
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`). The tests create and reset their own `Klasly_Tests` database; your `Klasly` database is never touched.
- No Brevo account and no internet.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run the new tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.EmailSenderTests
   ```
3. Expect `succeeded: 13`.
4. Run everything to be sure nothing else broke:
   ```
   dotnet test --solution Klasly.slnx
   ```
   Expect `succeeded: 84`.

## Example: what happens to one waiting email
| Brevo answers | Status | Attempts | Next try |
| --- | --- | --- | --- |
| Accepted | Accepted (messageId saved) | +1 | never again |
| 400 bad address | Failed (Brevo's message saved) | +1 | never again |
| 503 busy, 1st try | Pending | 1 | in 5–6 min |
| 503 busy, 2nd try | Pending | 2 | in 10–11 min |
| 503 busy, 5th try | Failed | 5 | never again (`MaxEmailAttempts` = 5) |
| 401 wrong API key | Pending, unchanged | 0 | the next tick; nothing else is sent this tick, and the Worker logs an error |
| (no API key set up) | Failed "Brevo is not configured" | unchanged | never again |

At most 50 emails go out per run. The rest wait for the next tick.

## Troubleshooting
- **`Cannot open database "Klasly_Tests"` / LocalDB errors**: start LocalDB with `sqllocaldb start MSSQLLocalDB`, then run the tests again.
- **`Setting 'MaxEmailAttempts' is missing or invalid`** (later, in the Worker log): someone changed that row in `SystemSettings`. Set it back to a whole number from 1 to 20 (default `5`).
- **A test fails only when run with the others**: a new test class that writes Email rows must be marked `[Collection("Outbox")]`, so it doesn't run at the same time as these.
