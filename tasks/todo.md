# Milestone 4 — Task list

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md)
> Build command for every task: `dotnet build Klasly.slnx`. No test project yet, so verification is by build plus a manual check with `src/Klasly.API/Klasly.API.http` (admin token from `admin@klasly.test` / `Klasly@123`).
> Every new controller gets `[Authorize(Policy = "AdminOnly")]` on the class. Every task adds its `.http` samples.

---

## Task 1: Error handling — `ConflictException` + `ApiExceptionHandler`

**Description:** One place that turns service exceptions into problem-details, so services can just throw and controllers stay one line. `KeyNotFoundException` → 404, `System.ComponentModel.DataAnnotations.ValidationException` → 400, `ConflictException` → 409, `DbUpdateException` from a unique index (SQL 2601/2627) → 409. Anything else stays a 500 and is logged.

**Acceptance criteria:**
- [x] `ConflictException(string message)` exists. `ApiExceptionHandler : IExceptionHandler` writes problem-details with the exception's message as `detail` for the three known types, and a generic "That value is already used." for the unique-index backstop.
- [x] `Program.cs` has `AddExceptionHandler<ApiExceptionHandler>()` and `app.UseExceptionHandler()`. Login (`/api/auth/login`) still behaves exactly as before.
- [x] Unknown exceptions keep the default 500 problem-details with no stack trace or SQL text in the body.

**Verification:**
- [x] Build succeeds
- [x] Manual: login 200 / wrong password 401 unchanged. Real 404/400/409 cases are checked in Task 2.

**Dependencies:** None

**Files likely touched:**
- `src/Klasly.API/ConflictException.cs` (new)
- `src/Klasly.API/ApiExceptionHandler.cs` (new)
- `src/Klasly.API/Program.cs`

**Estimated scope:** S (3 files)

---

## Task 2: Subjects slice

**Description:** `GET /api/subjects`, `GET /api/subjects/{id}`, `POST`, `PUT`. The first full slice, which sets the pattern the other resources copy.

**Acceptance criteria:**
- [x] `SubjectRequest { Code [Required, ≤20], Title [Required, ≤150], Units [Range 1–10] }` and `SubjectResponse { SubjectId, Code, Title, Units }`. List sorted by `Code`.
- [x] Duplicate code on create, or on update to another subject's code → `409` "Subject code 'X' is already used." Updating a subject to its own code is allowed. Unknown id → `404`.
- [x] `POST` returns `201` with a `Location` header (`CreatedAtAction` → `GET {id}`).

**Verification:**
- [x] Build succeeds
- [x] Manual: list shows ELECT3 and IT302; create → 201; duplicate `ELECT3` → 409; units 11 → 400; `GET /api/subjects/999` → 404; student token → 403; no token → 401

**Dependencies:** Task 1

**Files likely touched:**
- `src/Klasly.Core/DTOs/SubjectRequest.cs`, `SubjectResponse.cs` (new)
- `src/Klasly.Core/Interfaces/ISubjectRepository.cs` (new) + `src/Klasly.Infrastructure/Repositories/SubjectRepository.cs` (new) + `DependencyInjection.cs`
- `src/Klasly.API/Services/Interfaces/ISubjectService.cs`, `Services/SubjectService.cs` (new) + `Program.cs`
- `src/Klasly.API/Controllers/SubjectsController.cs` (new)
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** M (one slice; small files)

---

## Task 3: Terms slice with activate

**Description:** `GET /api/terms`, `GET {id}`, `POST`, `PUT`, `POST {id}/activate`, `DELETE {id}` (soft: `IsActive = false`). Activate makes this term the only active one.

**Acceptance criteria:**
- [x] `TermRequest { Name [Required, ≤50], StartDate, EndDate }` (`DateOnly`); `EndDate <= StartDate` → `400` "End date must be after start date." New terms start with `IsActive = false`. List sorted newest `StartDate` first. Duplicate name → `409`.
- [x] Activate clears every other active term with `ExecuteUpdateAsync`, then sets this one, inside one transaction. Activating the already-active term is a harmless `200`.
- [x] `DELETE` sets `IsActive = false` and returns `204`; the row stays.

**Verification:**
- [x] Build succeeds
- [x] Manual: create "2nd Sem 2026-27" → activate it → list shows only it active → activate "1st Sem 2026-27" again → only that one active (no 500 either time); bad dates → 400; delete → 204 and the term is still in the list with `isActive: false`

**Dependencies:** Task 1

**Files likely touched:**
- `src/Klasly.Core/DTOs/TermRequest.cs`, `TermResponse.cs` (new)
- `src/Klasly.Core/Interfaces/ITermRepository.cs` + `src/Klasly.Infrastructure/Repositories/TermRepository.cs` (new) + `DependencyInjection.cs`
- `src/Klasly.API/Services/Interfaces/ITermService.cs`, `Services/TermService.cs` (new) + `Program.cs`
- `src/Klasly.API/Controllers/TermsController.cs` (new)
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** M

---

## Checkpoint A — after Tasks 1–3
- [x] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [x] SPEC smoke steps 2, 3, 4, 10 pass for subjects and terms
- [x] Every duplicate gives `409` with a message, never `500`

---

## Task 4: Accounts — list, get, create

**Description:** `GET /api/accounts?role=`, `GET /api/accounts/{userId}`, `POST /api/accounts`. Create inserts the user and its role profile together. Also adds the teacher/student lookups that Tasks 6–8 reuse.

**Acceptance criteria:**
- [x] `CreateAccountRequest` (flat, per spec) with annotations; the service checks role-specific fields (Teacher: `employeeNo`; Student: `studentNumber`, `yearLevel` 1–6, `program`; Parent: `contactEmail` defaults to `email`; Admin: none) → `400` naming the missing field. Password hashed with `PasswordHasher<User>`. One `SaveChangesAsync`.
- [x] Duplicate email / student number / employee no. → `409` naming the value. `?role=` with an unknown value → `400`.
- [x] `AccountResponse` is flat with nullable profile fields and has **no** `PasswordHash`, `FcmToken`, `RegisteredDeviceId`. `IUserRepository` gains `ListAsync(role?)`, `GetByIdAsync(userId)` (tracked, includes profiles), `AddAsync`, `SaveChangesAsync`, the three "exists" checks, `GetTeacherAsync(teacherId)` and `GetStudentAsync(studentId)`.

**Verification:**
- [x] Build succeeds
- [x] Manual: create one Teacher, Student, Parent → 201 each; new student logs in → 200; student without `studentNumber` → 400; duplicate email → 409; duplicate student number → 409 **and** no new `Users` row (check `GET /api/accounts`); `?role=Teacher` lists 2 seeded + 1 new

**Dependencies:** Task 1

**Files likely touched:**
- `src/Klasly.Core/DTOs/CreateAccountRequest.cs`, `AccountResponse.cs` (new)
- `src/Klasly.Core/Interfaces/IUserRepository.cs` + `src/Klasly.Infrastructure/Repositories/UserRepository.cs`
- `src/Klasly.API/Services/Interfaces/IAccountService.cs`, `Services/AccountService.cs` (new) + `Program.cs`
- `src/Klasly.API/Controllers/AccountsController.cs` (new)
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** M

---

## Task 5: Accounts — update, deactivate, password reset

**Description:** `PUT /api/accounts/{userId}` and `DELETE /api/accounts/{userId}`. Update edits name, email, `isActive` and the profile fields for the account's role, and replaces the password hash when `newPassword` is given.

**Acceptance criteria:**
- [x] `UpdateAccountRequest` per spec (no `role`/`password`; `isActive`; optional `newPassword` min 8). Sets `UpdatedAt = DateTime.UtcNow`. Duplicate checks exclude the account itself.
- [x] `DELETE` → `IsActive = false`, `204`. Deactivating **your own** account (via `DELETE`, or `PUT` with `isActive: false`) → `400` "You cannot deactivate your own account."
- [x] `PUT` with `isActive: true` turns a deactivated account back on.

**Verification:**
- [x] Build succeeds
- [x] Manual: deactivate the new student → their login 401; reactivate → login 200; reset password → new one works, old one 401; admin deletes self → 400

**Dependencies:** Task 4

**Files likely touched:**
- `src/Klasly.Core/DTOs/UpdateAccountRequest.cs` (new)
- `src/Klasly.API/Services/Interfaces/IAccountService.cs`, `Services/AccountService.cs`
- `src/Klasly.API/Controllers/AccountsController.cs`
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** S (4 files)

---

## Task 6: Guardian links — list, link, unlink

**Description:** `GET`, `POST /api/accounts/{userId}/students` and `DELETE /api/accounts/{userId}/students/{studentId}` for Parent accounts. Unlink is the one approved hard delete.

**Acceptance criteria:**
- [x] `LinkStudentRequest { StudentId, Relationship? [≤30], ReceivesAlerts = true }`, `LinkedStudentResponse { StudentId, StudentNumber, StudentName, Relationship, ReceivesAlerts }`.
- [x] `userId` not a Parent account → `404`. Unknown `studentId` in the body → `400`. Already linked → `409`. Unlinking a pair that isn't linked → `404`.
- [x] Unlink removes only the `StudentGuardians` row; the student and parent accounts are untouched.

**Verification:**
- [x] Build succeeds
- [x] Manual: list a seeded parent's children; link the new parent to student 1 → 201; again → 409; unlink → 204; again → 404; `GET /api/accounts/1/students` (the admin) → 404

**Dependencies:** Task 4

**Files likely touched:**
- `src/Klasly.Core/DTOs/LinkStudentRequest.cs`, `LinkedStudentResponse.cs` (new)
- `src/Klasly.Core/Interfaces/IUserRepository.cs` + `src/Klasly.Infrastructure/Repositories/UserRepository.cs`
- `src/Klasly.API/Services/AccountService.cs` (+ interface)
- `src/Klasly.API/Controllers/AccountsController.cs`
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** S–M

---

## Checkpoint B — after Tasks 4–6
- [x] SPEC smoke steps 5, 6, 7 pass
- [x] No account response body contains `passwordHash`, `fcmToken` or `registeredDeviceId`
- [x] Team review before classes and enrollments

---

## Task 7: Classes slice

**Description:** `GET /api/classes?termId=`, `GET {id}`, `POST`, `PUT`. Responses carry subject, teacher and term names plus the active enrollment count.

**Acceptance criteria:**
- [x] `ClassRequest { SubjectId, TeacherId, TermId, Section [Required, ≤30], Schedule? [≤100], Room? [≤30] }`. Subject/teacher/term id that doesn't exist, or a teacher whose account is inactive → `400` naming it.
- [x] Same subject + section + term (excluding itself on update) → `409`. List sorted by subject code, then section; `?termId=` filters.
- [x] `ClassResponse` per spec, including `teacherName` and `activeEnrollmentCount` (Active only, Dropped not counted).

**Verification:**
- [x] Build succeeds
- [x] Manual: list shows the 2 seeded classes with the right enrollment counts; create → 201; duplicate → 409; `teacherId: 999` → 400; `?termId=1` filters

**Dependencies:** Tasks 2, 3, 4

**Files likely touched:**
- `src/Klasly.Core/DTOs/ClassRequest.cs`, `ClassResponse.cs` (new)
- `src/Klasly.Core/Interfaces/IClassRepository.cs` + `src/Klasly.Infrastructure/Repositories/ClassRepository.cs` (new) + `DependencyInjection.cs`
- `src/Klasly.API/Services/Interfaces/IClassService.cs`, `Services/ClassService.cs` (new) + `Program.cs`
- `src/Klasly.API/Controllers/ClassesController.cs` (new)
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** M

---

## Task 8: Enrollments slice

**Description:** `GET /api/classes/{classId}/enrollments`, `POST /api/enrollments`, `DELETE /api/enrollments/{id}` (soft: `Status = Dropped`). Enrolling a dropped student reactivates the old row.

**Acceptance criteria:**
- [x] `EnrollmentRequest { ClassId, StudentId }`, `EnrollmentResponse { EnrollmentId, ClassId, StudentId, StudentNumber, StudentName, Status }`. Unknown class/student, or inactive student account → `400`. Already Active → `409`.
- [x] New pair → `201`; existing Dropped pair → set `Active`, return `200`. `DELETE` → `Status = Dropped`, `204`.
- [x] `GET /api/classes/{classId}/enrollments` lists Active and Dropped, sorted by student name; unknown class → `404`.

**Verification:**
- [x] Build succeeds
- [x] Manual: enroll the new student → 201; again → 409; drop → 204; enroll again → 200 `status: Active`; class enrollment count in `GET /api/classes` matches

**Dependencies:** Tasks 4, 7

**Files likely touched:**
- `src/Klasly.Core/DTOs/EnrollmentRequest.cs`, `EnrollmentResponse.cs` (new)
- `src/Klasly.Core/Interfaces/IEnrollmentRepository.cs` + `src/Klasly.Infrastructure/Repositories/EnrollmentRepository.cs` (new) + `DependencyInjection.cs`
- `src/Klasly.API/Services/Interfaces/IEnrollmentService.cs`, `Services/EnrollmentService.cs` (new) + `Program.cs`
- `src/Klasly.API/Controllers/EnrollmentsController.cs` (new; the class-scoped list route lives here too)
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** S–M

---

## Checkpoint C — after Tasks 7–8
- [x] SPEC smoke steps 8, 9 pass

---

## Task 9: Full smoke test, package check, reports, commit

**Description:** Run all 10 SPEC smoke steps from a fresh database, confirm package versions, write the three reports (CLAUDE.md §5), mark the spec Done, and commit to `dev`.

**Acceptance criteria:**
- [x] All 10 SPEC smoke steps pass; row counts in Users, AcademicTerms, Subjects, Classes, Enrollments only went up. `dotnet list Klasly.slnx package` shows no Microsoft package below 10.x and no new packages.
- [x] `docs/reports/<yyyy-MM-dd-HHmm>-milestone-4-admin-crud/` has `DEVELOPER-DOCS.md`, `HOW-TO-USE.md`, `FLOWCHARTS.md`; `docs/reports/INDEX.md` has a new line.
- [x] Committed to `dev`, with no secrets in the diff.

**Verification:**
- [x] `git diff --cached` checked for keys before committing
- [x] Build succeeds

**Dependencies:** Tasks 1–8

**Files likely touched:**
- `docs/reports/<folder>/*.md` (3 new), `docs/reports/INDEX.md`
- `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` (status ticks)

**Estimated scope:** S

---

## Checkpoint D — complete
- [x] Every SPEC.md success criterion ticked
- [x] Ready for `/review` and for Milestone 5 (MVC login + Admin pages call these endpoints)
