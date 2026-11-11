# Milestone 4 — Admin CRUD · How to use

The admin can now set up a semester through the Web API: terms, subjects, accounts (with parent ↔ child links), classes and enrollments. There are no screens yet — those come in Milestone 5 (MVC Admin pages). For now you try it with the `.http` file or Swagger UI.

## Prerequisites
- **SQL Server LocalDB** running with the `Klasly` database (Milestone 2). To start from a clean seed:
  `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`
- **JWT key** in user-secrets (Milestone 3): `dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project src/Klasly.API`
- **The Web API** running: `dotnet run --project src/Klasly.API --launch-profile https`
- Not needed: Worker, MVC site, Android emulator.

## Step by step
1. Open `src/Klasly.API/Klasly.API.http` in Visual Studio or VS Code (REST Client).
2. Run **request 1 (Admin login)** and **request 2 (Student login)**. Every admin request below reuses the admin token automatically.
3. Scroll to the **Milestone 4** section and run the requests from top to bottom. Each title says the status you should get (for example `-> 201`, `-> 409`).
4. Or use Swagger UI: open `https://localhost:7256/swagger`, run `POST /api/auth/login` with `admin@klasly.test` / `Klasly@123`, copy the `token`, click **Authorize**, paste it, then try any endpoint.

A typical setup order for a new semester:
1. `POST /api/terms` → then `POST /api/terms/{id}/activate` to make it the current term.
2. `POST /api/subjects` for each subject.
3. `POST /api/accounts` for teachers, students and parents; `POST /api/accounts/{parentId}/students` to link each parent to their child.
4. `POST /api/classes` (subject + teacher + term + section).
5. `POST /api/enrollments` for each student in each class.

"Delete" is safe. Deactivating an account or term and dropping an enrollment only change a status, so the history stays. To undo, `PUT` the account with `"isActive": true`, activate the term again, or enroll the student again.

## Examples

**Create a student account**: `POST /api/accounts`
```json
{ "fullName": "Bianca Reyes", "email": "bianca.reyes@klasly.test", "password": "Student@123",
  "role": "Student", "studentNumber": "2026-00007", "yearLevel": 3, "program": "BSCpE" }
```
`201 Created`:
```json
{ "userId": 14, "fullName": "Bianca Reyes", "email": "bianca.reyes@klasly.test", "role": "Student",
  "isActive": true, "createdAt": "2026-11-09T12:04:44Z", "updatedAt": null,
  "studentId": 7, "studentNumber": "2026-00007", "yearLevel": 3, "program": "BSCpE",
  "teacherId": null, "employeeNo": null, "department": null,
  "guardianId": null, "contactEmail": null, "contactNumber": null }
```

**Required fields per role:** Teacher → `employeeNo`; Student → `studentNumber`, `yearLevel` (1–6), `program`; Parent → nothing extra (`contactEmail` defaults to the login email); Admin → nothing extra.

**List classes of a term**: `GET /api/classes?termId=1` → `200`:
```json
[{ "classId": 1, "subjectCode": "ELECT3", "subjectTitle": "Mobile Application Development",
   "teacherName": "Rosa Villanueva", "termName": "1st Sem 2026-27", "section": "BSCpE 3-A",
   "schedule": "MWF 07:30-09:00", "room": "CL-301", "activeEnrollmentCount": 6, "...": "..." }]
```

**Duplicate**: `POST /api/subjects` with `"code": "ELECT3"` → `409`:
```json
{ "title": "Conflict", "status": 409, "detail": "Subject code 'ELECT3' is already used." }
```

## Troubleshooting
| Problem | Fix |
|---|---|
| `401` on every request | The admin token is missing or expired (60 min). Run request 1 again, or click **Authorize** again in Swagger. |
| `403` on every request | You're using a non-admin token (teacher, student or parent). Only the Admin role can use these endpoints. |
| Sample ids don't match (e.g. `PUT /api/accounts/14` gives 404) | The `.http` ids assume a fresh seed and one run from top to bottom. Reset with `dotnet ef database drop -f -p src/Klasly.Infrastructure -s src/Klasly.API`, then `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`. |
| `400 "The JSON value could not be converted…"` on `role` | Send the role as text (`"Student"`), not a number. |
