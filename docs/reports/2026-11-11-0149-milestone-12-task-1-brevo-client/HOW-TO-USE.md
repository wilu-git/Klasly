# How to use — Milestone 12 Task 1 (Brevo email client)

There is nothing to click yet. This task adds the piece of the Worker that talks to Brevo; the Worker starts sending emails in Task 5, and real emails need the Brevo account from Task 6. You can check it by running its tests, which use a fake Brevo and never touch the internet.

## Prerequisites
- .NET 10 SDK. Nothing else: no database, no Brevo account, no internet for the tests.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run the new tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.BrevoEmailClientTests
   ```
3. Expect `succeeded: 15`.
4. Look at the new config section in `src/Klasly.Worker/appsettings.json`:
   ```json
   "Brevo": { "BaseUrl": "https://api.brevo.com/v3/", "SenderEmail": "", "SenderName": "Klasly" }
   ```
   `SenderEmail` stays empty until the team verifies a sender in Brevo (Task 6). The API key never goes in this file.

## Example: what Brevo's answers mean to the Worker
| Brevo answers | Outcome | What the Worker will do (Task 2) |
| --- | --- | --- |
| 201 `{ "messageId": "<…>" }` | Accepted | mark the email sent, keep the messageId |
| 400 (bad address, bad request) | Permanent | mark Failed with Brevo's message |
| 401 / 403 | Unauthorized | stop sending, keep the emails waiting, log "fix the API key" |
| 429 / 500 / 503, timeout, no network | Transient | try again later (5, 10, 20, 40 min…) |

## Troubleshooting
- **Restore fails for `Microsoft.Extensions.Http`**: you're offline on first build. Connect once so NuGet can download version 10.0.14.
- **`The type or namespace name 'Email' does not exist`**: you're on an older commit; pull the latest `dev`.
- **The timeout test is slow or flaky**: it waits 100 ms on purpose. Make sure nothing else is pinning the CPU, then run it again.
