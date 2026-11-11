# How to use: Admin › Failed emails (checked and polished)

The Failed emails page lists parent alert emails that Brevo refused or that ran out of retries, so the admin can fix the parent's address. This round only checked the page and fixed how it looks on a phone. It works the same as before.

## Prerequisites
- SQL Server LocalDB running with the `Klasly` database (migrations applied).
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (`https://localhost:7256`).
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https` (`https://localhost:7144`).
- The Worker is optional. It creates the failed rows, but the page only reads them.

## Step by step
1. Open `https://localhost:7144` and log in as the seeded admin, `admin@klasly.test` / `Klasly@123` (fictional test account).
2. Click **Failed emails** in the top bar.
3. Each row shows when the alert was created (Manila time), who it was for (name and email), the alert type and subject, how many send attempts were made, and the last error from Brevo.
4. To fix a bad address, go to **Accounts**, edit the parent's contact email, and save. New alerts go to the new address. Old failed rows stay in the list as a record.
5. **On a phone:** the table is wider than the screen, so swipe it sideways to see **Attempts** and **Last error**. The page itself doesn't move.

## Example
- **Desktop (1280 px):** one line per row. Last error reads "Brevo is not configured". See `1-admin-list.jpg`.
- **Phone (375 px):** Created and Recipient show first; swipe left for Alert, Attempts and Last error. A long Brevo error now wraps to about 9 short lines instead of 39. Compare `4-phone-375-before.jpg` with `5-phone-375-after.jpg`.
- **Teacher or parent typing the URL:** "You don't have access to that page." See `2-teacher-refused.jpg`.
- **No failed emails:** the page says "No failed emails. Every alert reached Brevo."

## Troubleshooting
| Problem | Fix |
| --- | --- |
| "Klasly can't reach the server right now." (`3-api-down.jpg`) | The Web API isn't running. Start it, then click **Try again**. |
| "You don't have access to that page." | You're logged in as a teacher, student or parent. Log out and log in as the admin. |
| `dotnet build` fails with "file is locked by Klasly.API / Klasly.Web" | The servers are still running and holding the files. Stop them (Ctrl+C), build, then start them again. |
