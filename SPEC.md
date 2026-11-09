# Spec: Milestone 4 — Web API admin CRUD

> Status: **Done — built 2026-11-09** (93/93 smoke checks pass). Report: `docs/reports/2026-11-09-2009-milestone-4-admin-crud/`. Approved 2026-11-09 with the recommended answers to the 3 open questions (unlink allowed, admin password reset added, no delete for Subjects/Classes). Plan: `tasks/plan.md`. Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec.
> Milestone 3's spec is in git history (commit `2e77723`) and in `docs/reports/2026-11-09-1906-milestone-3-auth/`.

## Assumptions (correct these before we build)

1. **Admin only.** Every endpoint in this milestone requires the `AdminOnly` policy from Milestone 3. Teachers, students and parents get their own read endpoints in later milestones (7, 13).
2. **"Delete" follows guide §27.7 exactly.** Only three things can be "deleted", and none of them removes a row:
   - Account → `IsActive = false` (that user can no longer log in — Milestone 3 already blocks it).
   - Term → `IsActive = false`.
   - Enrollment → `Status = Dropped`.
   **Subjects and Classes have no delete endpoint**, because their tables have no `IsActive` / `Status` column to soft-delete with, and adding one is a schema change (ask first).
3. **"Active term" and "soft-deleted term" are the same flag.** `AcademicTerms.IsActive` means "the current semester" (only one can be true — filtered unique index). So `DELETE /api/terms/{id}` just makes the term not current, and `POST /api/terms/{id}/activate` makes one term current and turns the others off, in one save.
4. **An account's role never changes after creation.** The role decides which profile row exists (Students / Teachers / Guardians). To "change" a role, deactivate the account and create a new one.
5. **The admin sets the first password** when creating an account. Minimum 8 characters. It is hashed with the same `PasswordHasher<User>` as login. The admin can reset it later with the optional `newPassword` field on update (decided: open question 2).
6. **An admin cannot deactivate their own account**, so the last admin cannot lock everyone out. → `400`.
7. **Guardian links are part of "accounts".** Admin links a parent to a student (`StudentGuardians`); the Parent portal (Milestone 13) needs these links to exist. **Unlinking deletes the join row** — the one hard delete in Klasly, allowed because a wrong link would expose another family's child (RA 10173) and a link row is not a record of anything that happened (decided: open question 1).
8. **No paging or search.** A school has hundreds of rows at most; lists return everything, sorted. Only the filters that the Milestone 5 admin pages need are added (`?role=`, `?termId=`).
9. **Ids in the URL that don't exist → `404`. Ids in the request body that don't exist → `400`** (for example "Subject 99 does not exist."), because the request itself is wrong, not the address.
10. **No test project yet** (same decision as Milestone 3). Testing is by hand with `Klasly.API.http`; automated role-access tests are Milestone 14.
11. **No database changes.** No migration. The Milestone 2 seed is used for testing.

## Objective

The admin sets up a school term before anyone can take attendance: the semester, the subject catalog, the user accounts, the class sections with their teacher, and which students sit in which class. Without this data nothing else in Klasly works — teachers have no classes to open, students have nothing to scan into, parents have no children to watch. This milestone builds the Web API side; Milestone 5 builds the MVC admin pages on top of it.

Functional requirement covered: guide §27.9 **#2** ("allow admins to manage academic terms, subjects, classes and enrollments"), plus account management from §27.1.

### User stories
- As the **admin**, I create the semester "1st Sem 2026-27" and mark it as the active term.
- As the **admin**, I add subjects (code, title, units) to the catalog and fix typos later.
- As the **admin**, I create accounts for teachers, students and parents with their profile details, and link each parent to their child.
- As the **admin**, I create a class (subject + section + term + teacher) and enroll students in it.
- As the **admin**, I drop a student from a class, or deactivate an account that should no longer log in — and the history stays in the database.
- As the **admin**, if I type a duplicate (same email, same student number, same subject code…) I get a clear message telling me which value is taken.

## API contract

All routes need `Authorization: Bearer <admin token>`. No token → `401`. Non-admin token → `403`.
Errors are problem-details. Validation errors (`400`) use the built-in `ValidationProblemDetails` from `[ApiController]` for missing/badly formatted fields, and plain problem-details with a `detail` message for rule failures.

### Terms — `/api/terms`

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `GET /api/terms` | — | `200` list, newest `StartDate` first | |
| `GET /api/terms/{id}` | — | `200` term | `404` |
| `POST /api/terms` | `{ name, startDate, endDate }` | `201` term (`isActive: false`) | `400` EndDate not after StartDate · `409` name taken |
| `PUT /api/terms/{id}` | `{ name, startDate, endDate }` | `200` term | `400` · `404` · `409` |
| `POST /api/terms/{id}/activate` | — | `200` term; every other term becomes `isActive: false` | `404` |
| `DELETE /api/terms/{id}` | — | `204`; sets `isActive: false` | `404` |

`TermResponse`: `{ termId, name, startDate, endDate, isActive }` — dates as `"2026-08-10"` (`DateOnly`).

### Subjects — `/api/subjects`

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `GET /api/subjects` | — | `200` list, by `code` | |
| `GET /api/subjects/{id}` | — | `200` subject | `404` |
| `POST /api/subjects` | `{ code, title, units }` | `201` subject | `400` units not 1–10 · `409` code taken |
| `PUT /api/subjects/{id}` | `{ code, title, units }` | `200` subject | `400` · `404` · `409` |

`SubjectResponse`: `{ subjectId, code, title, units }`. Limits: code ≤ 20, title ≤ 150.

### Classes — `/api/classes`

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `GET /api/classes?termId=` | — | `200` list (all terms if `termId` omitted), by subject code then section | |
| `GET /api/classes/{id}` | — | `200` class | `404` |
| `POST /api/classes` | `{ subjectId, teacherId, termId, section, schedule?, room? }` | `201` class | `400` subject/teacher/term does not exist, or teacher account inactive · `409` same subject + section + term exists |
| `PUT /api/classes/{id}` | same as POST | `200` class | `400` · `404` · `409` |

`ClassResponse`: `{ classId, subjectId, subjectCode, subjectTitle, teacherId, teacherName, termId, termName, section, schedule, room, activeEnrollmentCount }` — names included so the admin list page needs one call. Limits: section ≤ 30, schedule ≤ 100, room ≤ 30.

### Enrollments

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `GET /api/classes/{classId}/enrollments` | — | `200` list (Active and Dropped), by student name | `404` class |
| `POST /api/enrollments` | `{ classId, studentId }` | `201` new enrollment; **or `200`** if a Dropped row for that pair existed and was set back to Active | `400` class/student does not exist, or student account inactive · `409` already actively enrolled |
| `DELETE /api/enrollments/{id}` | — | `204`; sets `Status = Dropped` | `404` |

`EnrollmentResponse`: `{ enrollmentId, classId, studentId, studentNumber, studentName, status }`.
Re-enrolling reuses the old row because `(ClassId, StudentId)` is unique — a second row is impossible.

### Accounts — `/api/accounts`

| Method & route | Body | Success | Errors |
|---|---|---|---|
| `GET /api/accounts?role=` | — | `200` list (active and inactive), by full name; `role` = `Admin`/`Teacher`/`Student`/`Parent` | `400` unknown role |
| `GET /api/accounts/{userId}` | — | `200` account | `404` |
| `POST /api/accounts` | `CreateAccountRequest` | `201` account | `400` missing profile fields for that role · `409` email / student number / employee no. taken |
| `PUT /api/accounts/{userId}` | `UpdateAccountRequest` | `200` account | `400` (incl. deactivating yourself) · `404` · `409` |
| `DELETE /api/accounts/{userId}` | — | `204`; sets `IsActive = false` | `400` your own account · `404` |
| `GET /api/accounts/{userId}/students` | — | `200` the parent's linked students | `404` not a Parent account |
| `POST /api/accounts/{userId}/students` | `{ studentId, relationship?, receivesAlerts }` | `201` link | `400` student does not exist · `404` not a Parent account · `409` already linked |
| `DELETE /api/accounts/{userId}/students/{studentId}` | — | `204`; removes the link row | `404` not a Parent account, or not linked |

`CreateAccountRequest` — one flat shape; the service checks that the fields for the chosen role are filled:
```json
{
  "fullName": "Bianca Reyes",
  "email": "bianca.reyes@klasly.test",
  "password": "Klasly@123",
  "role": "Student",
  "studentNumber": "2026-00007", "yearLevel": 3, "program": "BSCpE",
  "employeeNo": null, "department": null,
  "contactEmail": null, "contactNumber": null
}
```
| Role | Required profile fields | Optional |
|---|---|---|
| Admin | — (no profile row) | |
| Teacher | `employeeNo` | `department` |
| Student | `studentNumber`, `yearLevel` (1–6), `program` | |
| Parent | — | `contactEmail` (defaults to the login `email`), `contactNumber` |

`UpdateAccountRequest`: the same fields **without** `role` and `password`, plus `isActive` (so a deactivated account can be turned back on) and an optional `newPassword` (min 8; when present the hash is replaced, when `null` the password is unchanged). Role-profile fields that don't match the account's role are ignored.

`AccountResponse` (flat; profile fields are `null` when they don't apply). **Never** contains `PasswordHash`, `FcmToken` or `RegisteredDeviceId`:
```json
{
  "userId": 9, "fullName": "Bianca Reyes", "email": "bianca.reyes@klasly.test",
  "role": "Student", "isActive": true, "createdAt": "2026-11-09T08:00:00Z", "updatedAt": null,
  "studentId": 7, "studentNumber": "2026-00007", "yearLevel": 3, "program": "BSCpE",
  "teacherId": null, "employeeNo": null, "department": null,
  "guardianId": null, "contactEmail": null, "contactNumber": null
}
```
`LinkedStudentResponse`: `{ studentId, studentNumber, studentName, relationship, receivesAlerts }`.

Creating an account inserts the `Users` row and its profile row in **one** `SaveChangesAsync`, so a half-created account is impossible.

## Commands

```bash
dotnet build Klasly.slnx
dotnet list Klasly.slnx package
dotnet run --project src/Klasly.API --launch-profile https   # Swagger UI at https://localhost:7256/swagger
```
No migration. Smoke tests in `src/Klasly.API/Klasly.API.http`.

## Project structure (files this milestone touches)

Follows the existing layout and guide §2: `Controller → Service → Repository → EF Core`. DTOs stay in `Klasly.Core/DTOs` like Milestone 3 (CLAUDE.md §3.2 overrides the `Klasly.API/Dtos` folder shown in guide §24).

```
src/Klasly.Core/
  DTOs/                     (new) request/response records per resource:
                            Term*, Subject*, Class*, Enrollment*, Account*, LinkStudent*, LinkedStudentResponse
  Interfaces/
    ITermRepository.cs      (new)
    ISubjectRepository.cs   (new)
    IClassRepository.cs     (new)
    IEnrollmentRepository.cs(new)
    IUserRepository.cs      (modified) list/get/add/update + student/teacher/guardian lookups + links
src/Klasly.Infrastructure/
  Repositories/             (new) TermRepository, SubjectRepository, ClassRepository, EnrollmentRepository
  Repositories/UserRepository.cs (modified)
  DependencyInjection.cs    (modified) register the new repositories
src/Klasly.API/
  Controllers/              (new) TermsController, SubjectsController, ClassesController,
                            EnrollmentsController, AccountsController
  Services/ + Interfaces/   (new) TermService, SubjectService, ClassService, EnrollmentService, AccountService
  ApiExceptionHandler.cs    (new) maps service exceptions to problem-details: not found → 404,
                            rule broken → 400, duplicate → 409 (built-in IExceptionHandler)
  Program.cs                (modified) register services + exception handler
  Klasly.API.http           (modified) one sample request per endpoint
```

No new packages. No generic repository, AutoMapper or MediatR (guide §21).

## Code style

Same as Milestone 3: file-scoped namespaces, primary constructors, records for DTOs, `async` + `CancellationToken` all the way down, DTOs out (never entities). Services throw; one exception handler turns that into the right status code, so controllers stay one line per action:

```csharp
namespace Klasly.API.Services;

public class SubjectService(ISubjectRepository subjects) : ISubjectService
{
    public async Task<SubjectResponse> CreateAsync(SubjectRequest request, CancellationToken ct)
    {
        if (await subjects.CodeExistsAsync(request.Code, excludeId: null, ct))
            throw new ConflictException($"Subject code '{request.Code}' is already used.");

        var subject = new Subject { Code = request.Code, Title = request.Title, Units = request.Units };
        await subjects.AddAsync(subject, ct);
        return subject.ToResponse();
    }
}
```

Duplicate checks happen in the service first so the admin gets a message naming the field. If two admins race and the database unique index fires anyway, the handler also maps that `DbUpdateException` to `409`.

## Testing strategy

- **Build:** `dotnet build` with 0 errors and 0 new warnings; `dotnet list package` shows no Microsoft package below 10.x.
- **Manual smoke test** against LocalDB with the Milestone 2 seed, using `Klasly.API.http` (admin token from `POST /api/auth/login`):
  1. Every list endpoint → `200` and shows the seeded rows.
  2. Teacher or student token on any admin route → `403`; no token → `401`.
  3. Term: create → activate → the previously active term now shows `isActive: false`. Create with EndDate ≤ StartDate → `400`.
  4. Subject: duplicate code → `409`; units = 11 → `400`.
  5. Account: create one of each role → `201`; Student without `studentNumber` → `400`; duplicate email → `409`; response has no `passwordHash`. Log in as the new student → `200`.
  6. Account: deactivate → that user's login now gives `401`. Admin deactivating self → `400`. Reset a password with `newPassword` → login works with the new one, fails with the old one.
  7. Parent link: link the new parent to a student → `201`; same link again → `409`; unlink → `204`; unlink again → `404`.
  8. Class: create → `201`; same subject + section + term → `409`; `teacherId: 999` → `400`.
  9. Enrollment: enroll → `201`; again → `409`; drop → `204`; enroll again → `200` with `status: Active`.
  10. Unknown id in the URL on any `GET/PUT/DELETE` → `404`.
- No automated tests (assumption 10).

## Boundaries

- **Always:** `[Authorize(Policy = "AdminOnly")]` on every controller in this milestone; soft delete only; UTC for `CreatedAt`/`UpdatedAt`; hash passwords with `PasswordHasher<User>`; follow Controller → Service → Repository; run `dotnet build`; write the three reports in `docs/reports/` when the milestone is built.
- **Ask first:** any schema change or migration (e.g. adding `IsActive` to Subjects or Classes); hard-deleting any row other than a `StudentGuardians` link; paging/search; adding a test project; any new package.
- **Never:** return an entity, `PasswordHash`, `FcmToken` or `RegisteredDeviceId`; log passwords; remove rows from Users, AcademicTerms, Enrollments, Subjects or Classes; turn on CORS; put business rules in controllers or repositories.

## Success criteria

- [x] All 10 smoke-test steps give the expected status codes.
- [x] Only one term is ever active, and activating a term never hits the unique-index error.
- [x] Every duplicate listed in guide §27.6 for these tables (term name, subject code, user email, student number, employee no., class subject+section+term, enrollment class+student, guardian link) returns `409` with a message naming the value — not a `500`.
- [x] No endpoint deletes a row except unlinking a guardian. After the smoke test, row counts in Users, AcademicTerms, Subjects, Classes, Enrollments only went up.
- [x] Non-admin tokens get `403` on every new route.
- [x] Reports written; `docs/reports/INDEX.md` updated.

## Decisions (were open questions — approved 2026-11-09)

1. **Unlinking a parent from a student:** allowed, as a hard delete of the `StudentGuardians` row (`DELETE /api/accounts/{userId}/students/{studentId}`).
2. **Admin password reset:** optional `newPassword` on `PUT /api/accounts/{userId}`.
3. **Subjects and Classes:** create and edit only; no delete, no migration.
