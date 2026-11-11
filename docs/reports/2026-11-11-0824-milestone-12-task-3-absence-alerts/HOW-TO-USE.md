# How to use — Milestone 12 Task 3 (Absence alerts)

This task adds the part of the Worker that writes an absence email for each parent who should get one. The email waits in the `Notifications` table until the email sender (Task 2) sends it. Both join the Worker's 5-minute tick in Task 5. For now you check it with the tests.

## Prerequisites
- .NET 10 SDK.
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`). The tests use their own `Klasly_Tests` database; your `Klasly` database is never touched.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run the new tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.AlertQueuerTests
   ```
   Expect `succeeded: 8`.
3. Run everything:
   ```
   dotnet test --solution Klasly.slnx
   ```
   Expect `succeeded: 92`.

## Example: who gets an absence email
Aira Mendoza (student 1) is marked Absent in an ELECT3 class. The Worker finalized that session less than 48 hours ago.

| Situation | Email queued? |
| --- | --- |
| Normal case: her mother Marites has alerts on | **Yes**: one email to Marites |
| The record is Excused | No |
| The session is only Closed, not Finalized yet | No (it comes after the Worker finalizes it) |
| The absence was marked more than 48 hours ago | No |
| Aira dropped the class | No |
| The class belongs to a past term | No |
| Marites turned alerts off, or her account is deactivated | No |
| The Worker runs again later | No second email |

What Marites will receive:
```
Subject: Klasly: Aira Mendoza was absent from ELECT3

Good day, Marites Mendoza.

Aira Mendoza was marked absent from ELECT3 Mobile Application Development, section A, in the class on Tue, 10 Nov 2026, 9:00 AM–10:30 AM (Asia/Manila).

The teacher marked or confirmed this absence in Klasly. If you have questions, please contact the teacher.

Klasly
```

## Troubleshooting
- **`Setting 'TimeZoneId' is missing or invalid`**: the `TimeZoneId` row in `SystemSettings` was changed or deleted. Set it back to `Asia/Manila`.
- **A parent didn't get an alert**: check that their link in `StudentGuardians` has `ReceivesAlerts = 1`, that their user is active, and that the student's enrollment is Active in the active term.
- **LocalDB errors when running the tests**: run `sqllocaldb start MSSQLLocalDB`, then try again.
