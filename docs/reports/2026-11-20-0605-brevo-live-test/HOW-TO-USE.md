# How to use — Brevo live emails from the Worker

The Worker now sends real parent alert emails through Brevo. This page shows how to set it up on your own machine and how to repeat the two tests we ran.

## Prerequisites
- .NET 10 SDK.
- SQL Server LocalDB with the `Klasly` database migrated (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- A Brevo account with a **verified sender** (Brevo → Senders, Domains & Dedicated IPs) and a **v3 API key** (Brevo → SMTP & API → API Keys). The MCP key is a different key and does not work here.
- `sqlcmd` (it comes with SQL Server tools), or SSMS / the VS SQL Server Object Explorer.
- The API and MVC site do **not** need to be running for these tests.

## Step-by-step

### 1. Give the Worker your key and sender
Run these yourself, in the solution folder. Don't paste the key into chat or into any file in the repo.
```bash
dotnet user-secrets set "Brevo:ApiKey" "<your-v3-api-key>" --project src/Klasly.Worker
```
```bash
dotnet user-secrets set "Brevo:SenderEmail" "<your-verified-sender@example.com>" --project src/Klasly.Worker
```
Check both are there:
```bash
dotnet user-secrets list --project src/Klasly.Worker
```

### 2. Point the dev guardians at real inboxes
Do this in **your local database only**. Any guardian left on `@klasly.test` would bounce.
```sql
UPDATE Guardians SET ContactEmail = '<your-email@gmail.com>' WHERE GuardianId IN (1,2);
UPDATE Guardians SET ContactEmail = '<teammate-email@gmail.com>' WHERE GuardianId IN (3,4);
```

### 3. Test 1: send a real email
1. Put one or two old alert rows back in the queue (pick IDs from `SELECT NotificationId, UserId, Type, Status FROM Notifications WHERE Channel='Email'`):
   ```sql
   UPDATE Notifications SET Status='Pending', Attempts=0, NextAttemptAt=NULL, LastError=NULL WHERE NotificationId IN (100,104);
   ```
2. Start the Worker:
   ```bash
   dotnet run --project src/Klasly.Worker
   ```
3. Wait for the next 5-minute mark on the clock (:00, :05, :10 …). The log shows `emails sent 2, retrying 0, failed 0`.
4. Check the rows: `Status` is `Accepted` and `ExternalMessageId` holds Brevo's message ID. Check the inboxes (and spam).
5. Stop the Worker with **Ctrl+C**.

### 4. Test 2: wrong key
1. Re-queue one row:
   ```sql
   UPDATE Notifications SET Status='Pending', Attempts=0, NextAttemptAt=NULL, LastError=NULL WHERE NotificationId = 102;
   ```
2. Start the Worker with a fake key. The environment variable beats user-secrets, so your real key stays as it is.
   - Git Bash:
     ```bash
     Brevo__ApiKey=xkeysib-wrong-key-for-test dotnet run --project src/Klasly.Worker
     ```
   - PowerShell:
     ```powershell
     $env:Brevo__ApiKey = "xkeysib-wrong-key-for-test"; dotnet run --project src/Klasly.Worker; Remove-Item Env:Brevo__ApiKey
     ```
3. At the next tick the log shows a red `ERR Brevo refused our API key or account (401 …)` and `emails sent 0, retrying 0, failed 0`.
4. Row 102 is still `Pending` with `Attempts = 0`. Nothing was lost; it will send once the key is right.
5. Stop the Worker (Ctrl+C). If you don't want that old alert sent later, put it back:
   ```sql
   UPDATE Notifications SET Status='Failed', LastError='Brevo is not configured' WHERE NotificationId = 102;
   ```

## Example
What a parent receives (plain text, from your verified sender, name "Klasly"):
> **Subject:** Klasly: Bryan Lacsamana was absent from ELECT3
> Bryan Lacsamana was marked absent from ELECT3 …, section …, in the class on ….

What the Admin sees: the row disappears from the Failed emails page, because it is now `Accepted`.

## Troubleshooting
| You see | Cause | Fix |
|---|---|---|
| `Brevo is not configured … due emails marked Failed` | `Brevo:ApiKey` or `Brevo:SenderEmail` is empty | Run step 1 again, then `dotnet user-secrets list` to check. |
| `ERR Brevo refused our API key or account (401 …)` with your real key | Wrong key type (the MCP key), a copy/paste space, or a deleted key | Make a new **v3** API key in Brevo → SMTP & API and set it again. |
| Row is `Failed` with a 400 message about the sender | The sender email isn't verified in Brevo | Verify it in Brevo, or set `Brevo:SenderEmail` to one that is. |
| Nothing happens for a few minutes | The job only runs on the 5-minute mark | Wait for :00/:05/:10…; the log prints a `Tick …` line each time. |
