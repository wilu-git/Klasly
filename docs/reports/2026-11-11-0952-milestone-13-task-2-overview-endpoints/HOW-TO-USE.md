# How to use — Milestone 13 Task 2 (student and parent overview endpoints)

The Web API can now answer "show me my records" for a student, and "show me my child" for a parent. The web pages and phone screens that use these come in Tasks 3–9; for now you can try them in Swagger or with `curl`.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database.
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (`https://localhost:7256`).
- Optional: the Worker running, so the risk fields are filled in.

## Step by step
1. Open `https://localhost:7256/swagger`.
2. Call `POST /api/auth/login` with a test account. All are fictional and use the password `Klasly@123`:
   - student: `aira.mendoza@klasly.test`
   - parent: `marites.mendoza@klasly.test` (children: Aira and Bryan)
3. Copy the `token` from the response, click **Authorize**, and paste it.
4. As the student: `GET /api/students/me/overview`.
5. As the parent: `GET /api/parent/children`, then `GET /api/parent/children/1/overview`.

## Example
`GET /api/parent/children/1/overview` (parent Marites), shortened:
```json
{
  "studentId": 1, "studentNumber": "…", "fullName": "Aira Mendoza", "termName": "1st Sem 2026-27",
  "classes": [{
    "subjectCode": "CPE 401", "section": "M5", "teacherName": "Rosa Villanueva",
    "present": 0, "late": 0, "absent": 2, "excused": 0,
    "absenceRate": 1.0, "weightedAverage": null, "isAtRisk": true, "riskComputedAtUtc": "…",
    "attendance": [{ "sessionId": 12, "startTimeUtc": "2026-11-10T01:00:00Z", "status": "Absent" }],
    "grades": [{ "title": "Quiz 1", "maxScore": 20, "weightPercent": 40, "score": null }]
  }]
}
```
The student's own call returns the same shape, except `isAtRisk` is always `null`.

## Troubleshooting
- **403 "You can only view your own children"**: that student isn't linked to the logged-in parent. The admin links parents on the Accounts page.
- **401**: the token is missing or expired. Log in again and re-authorize in Swagger.
- **`absenceRate`, `weightedAverage`, `isAtRisk` all null**: the Worker hasn't computed risk for that class yet. Run `dotnet run --project src/Klasly.Worker -- --Worker:Cron "* * * * *"` for a minute.
