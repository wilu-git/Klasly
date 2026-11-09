# Milestone 4 — Web API admin CRUD · Developer docs

## Summary
We asked for Milestone 4: admin CRUD in the Web API for terms, subjects, classes, enrollments and accounts, using soft delete. The work followed `SPEC.md` (approved with 3 decisions), then `tasks/plan.md` and `tasks/todo.md`.
27 admin-only endpoints now exist across five controllers. Each one follows Controller → Service → Repository → EF Core. Services throw exceptions, and one `IExceptionHandler` turns them into `400` / `404` / `409` problem-details. "Delete" never removes a row, except unlinking a parent from a student (the one approved hard delete). There are no database changes and no new packages.

## Projects touched
- **WEB API** (`Klasly.API`): 5 controllers, 5 services, exception handler, JSON enum setting, `.http` samples
- **EF CORE** (`Klasly.Core` + `Klasly.Infrastructure`): 16 DTOs, 4 new repositories, `IUserRepository` extended
- **MSSQL**: read/write through EF Core only. No migration.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.API/Middleware/ApiExceptionHandler.cs` | added | Maps `KeyNotFoundException`→404, `ValidationException`→400, `ConflictException`→409, unique-index `DbUpdateException`→409 |
| `src/Klasly.API/Services/ConflictException.cs` | added | The one custom exception: "value already taken" → 409 |
| `src/Klasly.API/Program.cs` | modified | Exception handler, enums as JSON strings, 5 new services |
| `src/Klasly.API/Controllers/{Subjects,Terms,Accounts,Classes,Enrollments}Controller.cs` | added | Endpoints; `[Authorize(Policy = "AdminOnly")]` on each class |
| `src/Klasly.API/Services/{Subject,Term,Account,Class,Enrollment}Service.cs` + `Interfaces/I…Service.cs` | added | All business rules for this milestone |
| `src/Klasly.Core/DTOs/*Request.cs`, `*Response.cs`, `AccountRequestBase.cs` | added | Request records with data annotations (lengths match `Klasly_Schema.sql`); response records with no entity or secret fields |
| `src/Klasly.Core/Interfaces/I{Subject,Term,Class,Enrollment}Repository.cs` | added | Data-access contracts |
| `src/Klasly.Core/Interfaces/IUserRepository.cs` | modified | List/get/add, exists checks, teacher/student lookups, guardian links |
| `src/Klasly.Infrastructure/Repositories/{Subject,Term,Class,Enrollment}Repository.cs` | added | EF Core implementations |
| `src/Klasly.Infrastructure/Repositories/UserRepository.cs` | modified | Implements the new `IUserRepository` members |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | Registers the 4 new repositories |
| `src/Klasly.API/Klasly.API.http` | modified | One or more sample requests per endpoint |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` | modified | Milestone 4 spec, plan, task list |

## Key code

**Endpoints** (all need an Admin JWT):

| Resource | Routes |
|---|---|
| Subjects | `GET/POST /api/subjects`, `GET/PUT /api/subjects/{id}` |
| Terms | `GET/POST /api/terms`, `GET/PUT/DELETE /api/terms/{id}`, `POST /api/terms/{id}/activate` |
| Accounts | `GET(?role=)/POST /api/accounts`, `GET/PUT/DELETE /api/accounts/{id}` |
| Guardian links | `GET/POST /api/accounts/{id}/students`, `DELETE /api/accounts/{id}/students/{studentId}` |
| Classes | `GET(?termId=)/POST /api/classes`, `GET/PUT /api/classes/{id}` |
| Enrollments | `GET /api/classes/{classId}/enrollments`, `POST /api/enrollments`, `DELETE /api/enrollments/{id}` |

**One handler, one-line controllers** (`ApiExceptionHandler.cs`):
```csharp
var (status, detail) = exception switch
{
    KeyNotFoundException e => (StatusCodes.Status404NotFound, e.Message),
    ValidationException e => (StatusCodes.Status400BadRequest, e.Message),
    ConflictException e => (StatusCodes.Status409Conflict, e.Message),
    DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } } =>
        (StatusCodes.Status409Conflict, "That value is already used."),
    _ => (0, null)
};
```

**Only one active term, without tripping the filtered unique index** (`TermRepository.ActivateAsync`). Clear the others first, then set this one, inside one transaction:
```csharp
await using var transaction = await context.Database.BeginTransactionAsync(ct);
await context.AcademicTerms
    .Where(t => t.IsActive && t.TermId != term.TermId)
    .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsActive, false), ct);
term.IsActive = true;
await context.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
```

**No half-created accounts** (`AccountService.CreateAsync`). The profile row rides on the user's navigation property, so EF inserts both in one `SaveChangesAsync`:
```csharp
var user = new User { FullName = request.FullName, Email = request.Email, Role = role, IsActive = true };
user.PasswordHash = hasher.HashPassword(user, request.Password);
switch (role) { case UserRole.Student: user.Student = new Student(); break; /* Teacher, Parent */ }
ApplyProfile(user, request);
await users.AddAsync(user, ct);
```

**Re-enrolling a dropped student reuses the row**, because `(ClassId, StudentId)` is unique (`EnrollmentService.EnrollAsync`). The result is `201` for a new row and `200` for a reactivated one.

**Other rules:**
- An admin cannot deactivate their own account (via `DELETE`, or `PUT` with `isActive: false`).
- A teacher or student with an inactive account cannot be given a class or an enrollment.
- `isActive` is required on account update, so leaving it out is a `400` instead of silently deactivating the account.
- Enums are sent and received as names (`"Student"`, `"Dropped"`). Numbers are rejected (`JsonStringEnumConverter(allowIntegerValues: false)`).

## Database changes
None. No migration. The Milestone 2 seed was used for testing.

## How it was verified
- `dotnet build Klasly.slnx` → **Build succeeded, 0 warnings, 0 errors**.
- `dotnet list Klasly.slnx package` → every Microsoft package is **10.0.14**. No package was added in this milestone.
- Fresh LocalDB (`dotnet ef database drop -f` + `database update`), API on `http://localhost:5201`, then a curl script with **93 status/value checks → 93 pass, 0 fail**. It covers every SPEC smoke step:
  - **Roles:** 401 with no token and 403 with a student token on every list.
  - **Duplicates:** 409 for each one (subject code, term name, email in a different case, student number, employee no., class subject+section+term, enrollment, guardian link).
  - **Terms:** activating switches the active term with exactly one active each time.
  - **Accounts:** a failed duplicate leaves no new `Users` row, and no response contains `passwordHash` / `fcmToken` / `registeredDeviceId`.
  - **Logins:** a deactivated user's login gives 401, and a reset password works while the old one fails.
  - **Enrollment counts:** 6 and 3 on the seeded classes; drop → count 0; re-enroll → same `enrollmentId 10`, status Active.
- Row counts in Users, AcademicTerms, Subjects, Classes and Enrollments only went up. LocalDB was reset to the seed afterwards.

## Known limits / TODO
- **No automated tests** (decided: test project in Milestone 14). The curl script is not committed.
- **Dates:** `DateTime` values read back from SQL are `Unspecified`. `AccountService` stamps them as UTC for the response (a `shortcut:` comment marks it). A global UTC value converter in `KlaslyDbContext` would fix this for every response; do it when sessions/attendance responses add more dates.
- **Not included:** no paging or search, no role change, no delete for Subjects/Classes (all by spec).
- **Race on duplicates:** if two admins save the same value at once, the backstop returns the generic message "That value is already used." instead of one that names the field.
