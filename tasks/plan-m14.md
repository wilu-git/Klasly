# Implementation Plan: Milestone 14 — Hardening: role-access tests, boundary tests, deploy to a host the phone can reach

> Built from [`SPEC.md`](../SPEC.md) (approved 2026-11-11, open questions 1–5 as recommended). Task details, acceptance criteria and verification steps are in [`todo-m14.md`](./todo-m14.md).
> Milestone 13 is in `plan-m13.md` / `todo-m13.md` (complete). `tasks/plan.md` / `todo.md` are Milestone 8's and are left untouched.

## Context

Milestones 1–13 built every Core feature. Rules and ownership are tested inside the services, but nothing tests the real HTTP pipeline (JWT → policy → status code), the scan endpoint, or session ownership. Real phones still need a USB cable to reach the API. Milestone 14 closes those gaps. It changes almost no production code: one fallback policy, one launch profile, and the small admin "Failed emails" list that M13 deferred here.

**Decisions (spec open questions, 2026-11-11):**
- Q1: add `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 to `tests/Klasly.Tests` only.
- Q2: "a host the phone can reach" = the team's PC on the same Wi-Fi / the phone's hotspot (`lan` launch profile + a firewall rule the team adds themselves).
- Q3: build the read-only admin "Failed emails" list, as the last feature task.
- Q4: no `TimeProvider` refactor; the late cut-off is tested ±1 minute.
- Q5: no MVC HTTP tests; the API is the security boundary.

## Architecture decisions

- **One in-memory API for HTTP tests.** `ApiFactory : WebApplicationFactory<Program>` points the API at the existing `Klasly_Tests` database (already reset once per run by `DatabaseFixture`). It uses fake keys set through `UseSetting`, because `Program.cs` reads `Jwt:Key` / `Qr:SigningKey` *before* `Build()`, and settings added later would arrive too late. The environment is `Testing`, so the developer's user-secrets and Swagger stay out.
- **Real logins, not hand-made tokens.** `ApiFactory.LoginAsync(email)` calls `POST /api/auth/login` with the seeded password and returns an `HttpClient` that carries the bearer token. One cached client per seeded account keeps the test run fast.
- **The endpoint list comes from the app.** `RoleAccessTests` reads `EndpointDataSource` and keeps only controller actions. It reads each action's `AuthorizeAttribute.Policy` to know which role is allowed, and fills `{id}` route values with `1`. New endpoints get tested automatically.
- **Fallback policy** `RequireAuthenticatedUser()` via `AddAuthorizationBuilder().SetFallbackPolicy(...)`. `[AllowAnonymous]` on Login still wins, and every current endpoint already requires login, so behavior doesn't change.
- **Scan tests at service level** (`AttendanceService` + `QrTokenService` built by hand over the test DB, as `GradeTests` does with its services). Tokens come from `QrTokenService.Create(sessionId, CurrentStep(...) - n, nonce)` using the real clock.
- **Failed emails** reuse the existing layers: a new `INotificationRepository` method returning a DTO (same pattern as `ListAbsenceAlertsAsync`), a thin service, an `AdminOnly` controller; on the MVC side an API-backed repository and an Admin area action + view.

## Reuse (don't rebuild)

- **Tests:**
  - `DatabaseFixture` (assembly fixture, `Klasly_Tests`, migrations + seed);
  - `TestData.NewClassAsync`, `RosaUserId` / `MarkUserId`;
  - the existing service-construction pattern in `GradeTests` / `AssessmentServiceTests`.
- **Seed (`KlaslySeed.cs`), password `Klasly@123`:**
  - `admin@klasly.test`;
  - teachers `rosa.villanueva@` (class 1) and `mark.delossantos@` (class 2);
  - students `aira.mendoza@` … `francis.nuqui@` (users 4–9);
  - parents `marites.mendoza@` (students 1, 2), `rodel.dimaano@` (student 3), `linda.bautista@`.
- **API:**
  - `ForbiddenException` / `ConflictException` / `FieldValidationException` → `ApiExceptionHandler`;
  - the `OnChallenge` problem-details 401 (`Program.cs:58-73`).
- **Web:**
  - the Admin area pattern (`[Area("Admin"), Authorize(Roles = "Admin")]`);
  - `AddHttpClient<…>` + `BearerTokenHandler`, `http.GetApiAsync<T>`, `ManilaTime`, `_Alerts`;
  - Admin nav tuples in `_Layout.cshtml:21`.
- **Android:** `klasly.apiBaseUrl` in `local.properties` (git-ignored); cleartext already allowed in the debug build.

## Dependency graph

```
T1 LAN profile + real phone   (independent, needs the team + a phone)
T2 ApiFactory + package ─┬─ T3 fallback policy + role sweep
                         ├─ T4 ownership tests
                         └─ T7 failed-emails API (its 401/403 is covered by the sweep)
T5 scan tests            (independent, DB only)
T6 boundary additions    (independent, DB only)
T7 failed-emails API ─── T8 failed-emails MVC page
```

## Task list

### Phase 1: Fail-fast foundation
- [ ] Task 1: API reachable from a real phone over Wi-Fi (`lan` profile + firewall + real-phone check) — XS
- [ ] Task 2: In-memory API test host (`ApiFactory`, package, smoke test) — S

### Checkpoint A
- [ ] `dotnet test` green. A real phone with no cable can log in through the PC's LAN address.

### Phase 2: Role and ownership
- [ ] Task 3: Fallback policy + endpoint sweep (no token → 401, wrong role → 403, scan → 401) — S
- [ ] Task 4: Ownership tests over HTTP (Rosa on Mark's class, unlinked child, not-enrolled scan) — S

### Phase 3: Scan and boundaries
- [ ] Task 5: Scan tests (7 checks in order, grace step, late cut-off ±1 min) — S
- [ ] Task 6: Boundary additions (risk end to end with seeded settings, MaxScore + 0.01) — S

### Checkpoint B
- [ ] `dotnet test` green. Every security and boundary criterion in the spec has a passing test. Review with the team.

### Phase 4: Failed-emails list
- [ ] Task 7: `GET /api/notifications/failed` (AdminOnly) + tests — M
- [ ] Task 8: MVC Admin › Failed emails page — M

### Checkpoint C (complete)
- [ ] All spec success criteria ticked; `dotnet build`, `dotnet test`, `dotnet list package` and `./gradlew assembleDebug` clean.
- [ ] Final real-phone run of the whole flow (scan → teacher's live list → student tabs → parent overview), with screenshots in the report.

## Parallelization

- Task 1 needs the team and a phone, and touches no code the others touch, so it can happen any time (best done early to surface network problems).
- Tasks 5 and 6 don't depend on Task 2 and touch different files; they could run alongside Tasks 3–4.
- Task 8 needs Task 7's contract (`FailedEmailResponse`). Everything else is sequential by preference: one test file per task keeps reviews small.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Campus Wi-Fi blocks phone-to-PC traffic (client isolation) | High (demo fails) | Use the phone's hotspot with the PC joined. Test it in Task 1, not on demo day. |
| Windows Firewall blocks port 5201, or the network is set to *Public* | High | The team runs the `New-NetFirewallRule … -Profile Private` command and sets the Wi-Fi profile to Private. The HOW-TO shows how to check both. |
| `WebApplicationFactory` settings arrive after `Program.cs` already read the keys → startup throws "Jwt:Key is missing" | Med | Use `builder.UseSetting(...)` (applied before the app's own code runs). Task 2's smoke test proves it first. |
| Top-level `Program` is internal, so the factory can't see it | Low | Add `public partial class Program;` at the end of `Program.cs` (one line). |
| The endpoint sweep hits an action whose body binding fails before authorization | Low | Authorization runs before model binding, so the 401/403 still comes first. The "right role" case only asserts "not 401/403". |
| HTTP tests and service tests share `Klasly_Tests`, and rows clash | Med | Tests that write make their own class/session (`TestData.NewClassAsync`). The sweep's "right role" calls are reads, or writes on throwaway rows. |
| A scan test straddles a 30 s QR step boundary and flakes | Low | Assert on step offsets (current, −1, −2) computed once per test, not on wall-clock seconds. |
| Swagger is reachable from the LAN (Development env on the `lan` profile) | Low | Accepted for the demo on a hotspot; noted in the report as a known limit. |

## Out of scope

- Cloud host / tunnel, release APK, HTTPS on the LAN.
- MVC HTTP tests, a `TimeProvider` refactor.
- A retry/delete button on failed emails.
- Serilog (in CLAUDE.md but not installed; flag it in the M15 review).

## Open questions

None. All five spec questions are answered as recommended.
