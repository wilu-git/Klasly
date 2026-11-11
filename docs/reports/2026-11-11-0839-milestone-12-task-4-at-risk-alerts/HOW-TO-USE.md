# How to use — Milestone 12 Task 4 (At-risk alerts)

The Worker can now write an email to parents when their child **becomes** at-risk in a class: too many absences, or a weighted average below passing (Milestone 11). The email waits in `Notifications` until the email sender sends it. Both start running on the Worker's tick in Task 5. For now you check it with the tests.

## Prerequisites
- .NET 10 SDK.
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`). The tests use their own `Klasly_Tests` database.

## Step by step
1. Open a terminal in the `Klasly` folder.
2. Run the alert tests:
   ```
   dotnet test --solution Klasly.slnx --filter-class Klasly.Tests.AlertQueuerTests
   ```
   Expect `succeeded: 15`.
3. Run everything:
   ```
   dotnet test --solution Klasly.slnx
   ```
   Expect `succeeded: 99`.

## Example: when does a parent get an at-risk email?
These are Aira's risk snapshots in ELECT3, oldest → newest (the Worker adds one only when her numbers change).

| Snapshots | Email? |
| --- | --- |
| at-risk (her first ever) | **Yes** |
| safe → at-risk | **Yes** |
| at-risk → at-risk (rate went up) | No: she was already flagged |
| at-risk → safe | No |
| at-risk → safe → at-risk | **Yes, a second one**: she relapsed |
| at-risk, but she dropped the class or it's a past term | No |

What her mother receives:
```
Subject: Klasly: Aira Mendoza may need support in ELECT3

Good day, Marites Mendoza.

Aira Mendoza may need support in ELECT3 Mobile Application Development, section A.

Absence rate: 25%
Weighted average: 72.50

This is an early-warning indicator for review, not a final grade. Please talk with Aira Mendoza and the teacher.

Klasly
```
If no grades are posted yet, the average line reads `Weighted average: no grades yet`.

## Troubleshooting
- **A parent got no at-risk email though the dashboard shows at-risk**: the student was probably already at-risk in the previous snapshot. Only the change to at-risk sends an email.
- **`Setting 'TimeZoneId' is missing or invalid`**: the alert step reads it first. Set `TimeZoneId` back to `Asia/Manila` in `SystemSettings`.
- **A test fails only when the whole suite runs**: a new test class that writes Email rows or RiskFlags must not race with these. Put it in `[Collection("Outbox")]`, or date its flags so they can't clash.
