# How to use — Brevo setup for Klasly (Milestone 12 Task 6)

This is the step-by-step guide to create a Brevo account and connect it to Klasly's Worker, so parents' absence and at-risk alerts are really emailed. It also covers what this fix changed: if Brevo says the account is out of credits or not activated yet, the emails now **wait** instead of failing for good.

> Brevo's website changes its menus from time to time. If a menu name below doesn't match exactly, look for the closest one. The steps stay the same.

## Prerequisites
- A team member's own email address, used as the sender and to receive the test alert. Never use a real parent's address (RA 10173).
- The `Klasly` dev database is up to date (`dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`).
- .NET 10 SDK and `sqlcmd` (both already used in this project).

## Part A — Create the Brevo account (website)
1. Go to **https://www.brevo.com** and choose **Sign up free**. Use the team member's own email.
2. Open the confirmation email from Brevo and click the link.
3. Fill in the profile Brevo asks for: name, organization (for example "Klasly – ELECT3 project"), and address. Brevo may also ask for a phone number and send an SMS code. The free plan sends **300 emails a day**.
4. New accounts can be **under review** for a short time before they may send. Until then, Brevo answers with an "account under validation" or 402 error. Klasly now treats that as an account problem: emails stay **Pending** and go out after activation.

## Part B — Add and verify a sender
5. In Brevo, open **Settings** (gear icon) → **Senders, domains & dedicated IPs** → **Senders** → **Add a sender**.
6. Enter name **Klasly** and the team member's email address, then save.
7. Brevo emails a confirmation link or code to that address. **Confirm it.** Do this **before** adding the API key to Klasly. Brevo's docs don't say what an unverified sender returns, and if it's a plain 400, those emails would be marked Failed.
8. *(Note)* A free address such as Gmail can be used for a class demo, but its emails may land in **Spam**, because you can't authenticate gmail.com. A real deployment should use the school's own domain, authenticated under **Domains** (DKIM/DMARC).

## Part C — Create the API key
9. Open the profile menu (top right) → **SMTP & API** → **API Keys** tab → **Generate a new API key**.
10. Name it `klasly-worker-dev` and **copy it now**; Brevo shows it only once. It starts with `xkeysib-`. Don't use the *SMTP* key (`xsmtpsib-…`), which won't work with the API.
11. Never paste the key into a file in the repository, a chat, or a screenshot.

## Part D — Connect it to the Worker (on each developer's computer)
12. Open a terminal in the `Klasly` folder.
13. Save the key and the sender in **user-secrets**. They are stored on your computer, outside the repo:
    ```
    dotnet user-secrets set "Brevo:ApiKey" "xkeysib-...your key..." --project src/Klasly.Worker
    dotnet user-secrets set "Brevo:SenderEmail" "your.verified.sender@example.com" --project src/Klasly.Worker
    ```
    `SenderEmail` must be **exactly** the address verified in step 7. Keep it in user-secrets, not appsettings: it's a real person's address and must not be committed.
14. Check that both are set. This prints the key, so don't share the output:
    ```
    dotnet user-secrets list --project src/Klasly.Worker
    ```
15. Each teammate who runs the Worker repeats steps 13–14 on their own computer; user-secrets don't travel with git. Share the key privately, or let each person generate their own.

## Part E — Make the dev database safe to send from
16. The dev database has **4 guardians** with fake `@klasly.test` addresses. Once the key works, an alert to a `.test` address would bounce and hurt the sender's reputation. Point all of them to team inboxes. Plus-addressing (`name+marites@gmail.com`) delivers to the same inbox but shows who it was for:
    ```
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -E -Q "UPDATE Guardians SET ContactEmail = CASE GuardianId WHEN 1 THEN 'you+marites@example.com' WHEN 2 THEN 'you+rodel@example.com' WHEN 3 THEN 'you+linda@example.com' ELSE 'you+other@example.com' END"
    ```
    Replace `you@example.com` with your own address. This changes the **dev database only**, never the seed code.

## Part F — Send a real alert
17. Create a fresh absence for Aira (student 1, guardian 1) in ELECT3 (class 1). It is a new session, so it gets a new alert. The 108 alerts from the earlier trial run stay Failed on purpose.
    ```
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -E -Q "DECLARE @now datetime2 = SYSUTCDATETIME(); INSERT INTO ClassSessions (ClassId, StartTime, EndTime, QrNonce, Status, ClosedAt) VALUES (1, DATEADD(minute,-90,@now), DATEADD(minute,-5,@now), REPLACE(CONVERT(nvarchar(36), NEWID()), '-', ''), N'Finalized', @now); INSERT INTO AttendanceRecords (SessionId, StudentId, Status, MarkedAt, Source) VALUES (SCOPE_IDENTITY(), 1, N'Absent', @now, N'Auto');"
    ```
    *(You can also use the app instead: open a short session in the Teacher portal and let it end without Aira scanning. The Worker then marks the whole class Absent, so every linked guardian gets an email.)*
18. Run the Worker with a one-minute tick:
    ```
    dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"
    ```
19. Within a minute, the log should show:
    ```
    Tick …: queued 1 absence and 0 at-risk emails
    Tick …: emails sent 1, retrying 0, failed 0
    ```
20. Check the row; it should be `Accepted` with Brevo's message id:
    ```
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -E -Q "SELECT TOP 3 NotificationId, Status, Attempts, ExternalMessageId, LastError, DedupKey FROM Notifications WHERE Channel = 'Email' ORDER BY NotificationId DESC"
    ```
21. Open the inbox and check **Spam** too. You should see "Klasly: Aira Mendoza was absent from ELECT3". Brevo's **Transactional → Logs** page also lists it.

## Part G — Check the wrong-key behaviour (spec Q2)
22. Stop the Worker (`Ctrl+C`). Set a wrong key, then repeat step 17 for a new absence and run the Worker again:
    ```
    dotnet user-secrets set "Brevo:ApiKey" "xkeysib-wrong" --project src/Klasly.Worker
    ```
23. The log shows `Brevo refused our API key or account (401 …). Emails wait until it is fixed.` The row stays **Pending** with **Attempts 0**.
24. Put the real key back (step 13) and run the Worker. The waiting email is sent on the next tick.
25. Tick the Task 6 boxes in `tasks/todo-m12.md`.

## Example: what Klasly does with each Brevo answer (after this fix)
| Brevo answers | Klasly does |
| --- | --- |
| 201 + messageId | Accepted |
| 400 bad address | Failed for good (that email is wrong) |
| **402**, or 400 `not_enough_credits` / `account_under_validation` | **stops for this tick; emails wait** (new) |
| 401 / 403 | stops for this tick; emails wait |
| 429, 5xx, timeout | tries again in 5, 10, 20… minutes |

## Troubleshooting
- **`401 … unrecognised IP address`** in the log: Brevo blocks API calls from IP addresses it hasn't seen. In Brevo, open **Security → Authorised IPs** and authorize your IP, or turn the blocking off while developing. The emails wait in the meantime.
- **`Brevo is not configured` warning**: `Brevo:ApiKey` or `Brevo:SenderEmail` is missing. Run step 14 to check. The secrets must be set for `--project src/Klasly.Worker`, not the API project.
- **`Setting 'Brevo:BaseUrl' is invalid`** at startup: someone changed `Brevo:BaseUrl`. It must be an https URL; leave it as `https://api.brevo.com/v3/`.
- **Email "sent" but not in the inbox**: look in Spam, and check Brevo's Transactional logs for a bounce. Gmail senders are often filtered (step 8).
