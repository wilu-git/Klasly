# How to use — Milestone 13 Task 4 (Parent portal pages)

Parents can log into the Klasly website and see their children's attendance, grades and at-risk status for this term.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database.
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https`.
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https` (`https://localhost:7144`).
- Optional: the Worker running (`dotnet run --project src/Klasly.Worker`), so the at-risk badges are up to date.

## Step by step
1. Open `https://localhost:7144` and log in as a test parent: `marites.mendoza@klasly.test`, password `Klasly@123` (fictional).
2. You land on **My children**. Click **View** next to a child.
3. Each card is one class:
   - **badge:** At risk (red), On track (green), or Not computed yet (grey);
   - **Attendance:** how many sessions the child was Present, Late, Absent or Excused;
   - **Absence rate** and **Weighted average**, from the latest check;
   - click **Attendance records** or **Grades** to expand the details.
4. With more than one child, use the name buttons at the top to switch.

## Example
A card for one class:
> **ELECT3** Mobile Application Development · BSCpE 3-A · Rosa Villanueva — **At risk**
> Attendance: Present 4 · Late 1 · Absent 12 · Excused 1 · Absence rate 75% · Weighted average 90
> *This is an early-warning indicator for review, not a final grade. Updated Nov 11, 12:52 AM.*

## Troubleshooting
- **"You can only view your own children"**: the link was for a child who isn't linked to your account. The admin links parents and children on Admin › Accounts.
- **"Klasly can't reach the server right now"**: the Web API isn't running, or is still starting. Wait a few seconds and try again.
- **The badge says "Not computed yet"**: the Worker hasn't checked that class yet. Start the Worker and reload after a few minutes.
