# Implementation Plan: Milestone 4 — Web API admin CRUD

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-09 with the recommended answers to the 3 open questions). Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).

## Overview

Add admin-only REST endpoints to `Klasly.API` for terms, subjects, accounts (with guardian links), classes and enrollments. Each resource is one **vertical slice**: DTOs → repository → service → controller → `.http` samples, so after every task there is something you can call and check. There are no database changes: everything runs on the Milestone 2 seed (1 active term, 2 subjects, 2 teachers, 6 students, 3 parents, 2 classes).

## Dependency graph

```
Task 1  Error handling (ConflictException + ApiExceptionHandler)
   │
   ├── Task 2  Subjects slice ─────────────┐
   ├── Task 3  Terms slice (+ activate) ───┤
   │                                       │
   ├── Task 4  Accounts: list / get / create
   │      │
   │      ├── Task 5  Accounts: update / deactivate / password reset
   │      └── Task 6  Guardian links (list / link / unlink)
   │                                       │
   └── Task 7  Classes slice  ◄── needs Subjects (2), Terms (3), teacher lookup (4)
          │
          └── Task 8  Enrollments slice  ◄── needs student lookup (4)
                 │
                 └── Task 9  Full smoke test, reports, commit
```

Tasks 2, 3 and 4 depend only on Task 1. They all edit `DependencyInjection.cs`, `Program.cs` and `Klasly.API.http`, so we build them **one after another** in one session, not in parallel.

## Architecture decisions

| Decision | Why |
|---|---|
| Layer flow `XController → IXService → IXRepository → EF Core` for every resource | Guide §2 and §21. No generic repository, no AutoMapper. |
| Repository interfaces in `Klasly.Core/Interfaces`, EF Core implementations in `Klasly.Infrastructure/Repositories`, services in `Klasly.API/Services` | Same places as Milestone 3 (CLAUDE.md §3.2). |
| DTOs in `Klasly.Core/DTOs`, one record per file | Matches `LoginRequest.cs` / `LoginResponse.cs`. Android and MVC will mirror these shapes. |
| Request validation with data annotations on the request records (`[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[MinLength(8)]`) | `[ApiController]` already turns them into `400` validation problem-details. Lengths match `Klasly_Schema.sql`. |
| Rules that need the database (duplicates, "does this id exist", one active term, self-deactivation) live in the **services** | Guide §3: business rules in services; repositories hold no rules. |
| Services **throw**: `KeyNotFoundException` → 404, `ValidationException` → 400, `ConflictException` → 409. One `ApiExceptionHandler : IExceptionHandler` turns them into problem-details | Guide §3 example already throws `KeyNotFoundException` / `ValidationException`. `IExceptionHandler` is built into ASP.NET Core 10, so controllers stay one line per action. Only `ConflictException` is a new class. |
| The handler also maps `DbUpdateException` with SQL error 2601/2627 (unique index) → 409 | Backstop if two admins save the same value at once. The service's own check gives the nicer message. |
| Repositories: `ListAsync` uses `AsNoTracking()`; `GetByIdAsync` returns a **tracked** entity; the service changes properties then calls `SaveChangesAsync()` | Guide §7: no `Update()` / `Attach()` on entities the context already tracks. |
| Entity → response mapping is a `private static ToResponse(...)` inside each service | Guide §3 ("services transform data"). One place per resource, no mapper library. |
| Class and enrollment queries use `Include` for subject / teacher / term / student names; `activeEnrollmentCount` uses a filtered include | One query per list; spec wants names in the response. |
| Term activate = `ExecuteUpdateAsync` to clear every other active term, then set this one, inside one transaction | The filtered unique index allows one `IsActive = 1`. A single `SaveChanges` with two updates can run "set true" first and break the index. Clearing first avoids that. |
| Create account = add `User` **with** its profile (`Student`/`Teacher`/`Guardian`) as a navigation property, one `SaveChangesAsync()` | EF inserts both in one transaction: no half-created account. |
| Self-deactivation check: controller reads `sub` from the token and passes it to the service | Same claim `/api/auth/me` already reads. The rule itself stays in the service. |
| Teacher / student lookups (`GetTeacherAsync`, `GetStudentAsync`) live on `IUserRepository` | Classes, enrollments and guardian links all need them. One place instead of three. Guardian link methods live there too (links belong to accounts in the spec). |
| `[Authorize(Policy = "AdminOnly")]` on each new **controller class** | One attribute per controller; no route can be forgotten. |

## Task list

### Phase 1: Foundation and simple slices
- [x] Task 1: Error handling — `ConflictException` + `ApiExceptionHandler` (S)
- [x] Task 2: Subjects slice (M)
- [x] Task 3: Terms slice with activate (M)

### Checkpoint A — after Tasks 1–3
- [x] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [x] SPEC smoke steps 2, 3, 4 and 10 pass for subjects and terms
- [x] A duplicate gives `409` with the message, never `500`

### Phase 2: Accounts
- [x] Task 4: Accounts — list, get, create (M)
- [x] Task 5: Accounts — update, deactivate, password reset (S)
- [x] Task 6: Guardian links — list, link, unlink (S)

### Checkpoint B — after Tasks 4–6
- [x] SPEC smoke steps 5, 6 and 7 pass
- [x] No response contains `passwordHash`, `fcmToken` or `registeredDeviceId`
- [x] Team review before classes and enrollments

### Phase 3: Classes and enrollments
- [x] Task 7: Classes slice (M)
- [x] Task 8: Enrollments slice (S–M)

### Checkpoint C — after Tasks 7–8
- [x] SPEC smoke steps 8 and 9 pass

### Phase 4: Wrap-up
- [x] Task 9: Full smoke test, package check, reports, commit (S)

### Checkpoint D — complete
- [x] Every SPEC.md success criterion ticked
- [x] `dotnet list package` shows no Microsoft package below 10.x
- [x] Reports in `docs/reports/`, `INDEX.md` updated, committed to `dev`

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Activating a term trips `UX_AcademicTerms_OneActive` | High: 500 on a core admin action | Clear the others with `ExecuteUpdateAsync` first, in one transaction (decision above). Task 3 tests activating twice in a row and switching between two terms. |
| Account + profile saved separately, leaving a `User` with no profile | High: a student who can log in but has no `StudentId` | Single `SaveChangesAsync` via navigation property. Task 4 checks a forced duplicate student number leaves **no** new `Users` row. |
| A `DbUpdateException` from a CHECK constraint (not a unique index) leaks as 500 | Med | Request annotations cover every CHECK (units 1–10, year 1–6, dates). The handler maps only 2601/2627 to 409; anything else stays a 500 and is logged, so we notice it. |
| Leaking `PasswordHash` through a response | High: security | Responses are hand-written records with no hash field. Checkpoint B greps a response body for `passwordHash`. |
| Filtered include for `activeEnrollmentCount` combined with `AsNoTracking` behaves unexpectedly | Low | Checked in Task 7 against the seed (each seeded class has a known enrollment count). If it misbehaves, count with a projection in the repository instead. |
| `DependencyInjection.cs` / `Program.cs` edited by every task → merge mess if split across sessions | Low | Build the tasks in order in one session. |

## Out of scope (from SPEC.md)

Paging and search, delete for Subjects and Classes, changing an account's role, teacher/student/parent read endpoints (Milestones 7 and 13), MVC admin pages (Milestone 5), test project (Milestone 14), Serilog.

## Open questions

None. The three spec questions were answered on 2026-11-09 (see SPEC.md "Decisions").
