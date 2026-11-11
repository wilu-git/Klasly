# How to use — risk settings range check

The Worker now refuses risk settings that are clearly typed in the wrong scale, instead of quietly flagging nobody.

## Prerequisites
- The `Klasly` database on LocalDB and the Worker (`dotnet run --project src/Klasly.Worker`).

## Step by step
1. The two settings live in the `SystemSettings` table:
   - `AbsenceThreshold`: a fraction above 0 and up to 1. `0.20` means 20%.
   - `PassingGrade`: a grade from 1 to 100. `75` is the default.
2. To change one, update the row, for example:
   ```
   sqlcmd -S "(localdb)\MSSQLLocalDB" -d Klasly -Q "UPDATE SystemSettings SET Value = '0.25', UpdatedAt = SYSUTCDATETIME() WHERE [Key] = 'AbsenceThreshold'"
   ```
3. The next Worker tick uses the new value.

## Example
| Value typed | Result |
| --- | --- |
| `AbsenceThreshold = 0.25` | OK: students missing 25% or more are flagged |
| `AbsenceThreshold = 20` | Refused: `Setting 'AbsenceThreshold' is missing or invalid: it must be a fraction above 0 and up to 1, e.g. 0.20.` |
| `PassingGrade = 0.75` | Refused: `… it must be a grade from 1 to 100, e.g. 75.` |

While a setting is refused, the risk step writes nothing; earlier flags stay as they were.

## Troubleshooting
- **The Worker log shows the "missing or invalid" message every tick**: fix the value as in the table above. The next tick works again.
- **Using a comma for decimals (`0,20`)**: not accepted. Use a dot.
- **The closing and finalizing of sessions still happen while a setting is bad**: that's expected. Only the risk step stops.
