# Developer docs: Milestone 14 — Hardening

## Summary
We asked for every task in `tasks/plan-m14.md` to be built. What changed:
- The Web API can now be tested in memory over real HTTP (`WebApplicationFactory`).
- New tests cover:
  - every endpoint against every role, found from the app itself;
  - ownership (another teacher's class, a parent's unlinked child, a scan for a class the student isn't in);
  - scan checks 2 to 7, the QR grace step and the late cut-off;
  - exactly 20% and exactly 75 through the real risk calculation.
- The API now requires login by default (fallback policy).
- A `lan` launch profile lets a phone on the same Wi-Fi reach the API.
- Admins have a read-only Failed emails list, in the API and on the website.

Tests went from 121 (before this milestone) to 154, all green.

## Projects touched
WEB API · WEB MVC · EF CORE (one repository query, no schema change) · tests. Not touched: CONSOLE WORKER, ANDROID KOTLIN, MSSQL schema.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.API/Properties/launchSettings.json` | modified | `lan` profile, listens on `http://0.0.0.0:5201` |
| `src/Klasly.API/Program.cs` | modified | fallback policy (login required); `public partial class Program;` for the test host; registers `INotificationService` |
| `tests/Klasly.Tests/Klasly.Tests.csproj` | modified | + `Microsoft.AspNetCore.Mvc.Testing` 10.0.14 |
| `tests/Klasly.Tests/DatabaseFixture.cs` | modified | connection string made `internal` so the API host can share it |
| `tests/Klasly.Tests/ApiFactory.cs` | added | in-memory API on `Klasly_Tests` with fake keys, plus a cached login helper |
| `tests/Klasly.Tests/ApiFactoryTests.cs` | added | smoke test: admin logs in, `auth/me` says Admin |
| `tests/Klasly.Tests/RoleAccessTests.cs` | added | endpoint sweep (no token, wrong role, own role, role-policy list, scan as a non-student) |
| `tests/Klasly.Tests/OwnershipTests.cs` | added | 13 endpoints on Mark's class as Rosa, an unlinked child, a scan with no enrollment |
| `tests/Klasly.Tests/ScanTests.cs` | added | checks 2–7, the grace step, the late cut-off |
| `tests/Klasly.Tests/TestData.cs` | modified | + `NewSessionAsync` |
| `tests/Klasly.Tests/RiskCalculatorTests.cs` | modified | + exact 20% / 75 test with the seeded settings |
| `tests/Klasly.Tests/FailedEmailsTests.cs` | added | only Failed Email rows, with the address and last error |
| `src/Klasly.Core/DTOs/FailedEmailResponse.cs` | added | API response record |
| `src/Klasly.Core/Interfaces/INotificationRepository.cs`, `src/Klasly.Infrastructure/Repositories/NotificationRepository.cs` | modified | + `ListFailedEmailsAsync` (a projection) |
| `src/Klasly.API/Services/(Interfaces/I)NotificationService.cs` | added | the service layer between controller and repository |
| `src/Klasly.API/Controllers/NotificationsController.cs` | added | `GET /api/notifications/failed` (AdminOnly) |
| `src/Klasly.Web/Models/Api/NotificationDtos.cs` | added | MVC copy of the DTO |
| `src/Klasly.Web/Repositories/(Interfaces/I)NotificationRepository.cs`, `Services/(Interfaces/I)NotificationService.cs` | added | API-backed repository + service |
| `src/Klasly.Web/Areas/Admin/Controllers/FailedEmailsController.cs`, `Areas/Admin/Views/FailedEmails/Index.cshtml` | added | Admin › Failed emails page |
| `src/Klasly.Web/Program.cs`, `Views/Shared/_Layout.cshtml` | modified | client registration, nav item |

## Key code

**Fallback policy** (`src/Klasly.API/Program.cs`): a controller that forgets `[Authorize]` still needs a login.
```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin")) …
```

**Test host** (`ApiFactory.cs`): `UseSetting`, because `Program.cs` reads the keys before `Build()`.
```csharp
builder.UseEnvironment("Testing");
builder.UseSetting("ConnectionStrings:Klasly", DatabaseFixture.ConnectionString);
builder.UseSetting("Jwt:Key", "test-only-jwt-key-…");
```

**Endpoint sweep** (`RoleAccessTests.cs`): endpoints are read from `EndpointDataSource`, and `{id}` becomes `1`.
- No token → 401 everywhere except login.
- Each wrong role → 403.
- Own role (GETs only, so nothing is changed or deleted) → never a policy refusal. A policy 403 has an empty body; an ownership 403 has a message, so the two can be told apart.
- Only `auth/login`, `auth/me` and `attendance/scan` may have no role policy. A temporary test proved it: removing `[Authorize]` from `SubjectsController` turns this test red and names `GET /api/subjects`.

**Endpoint:** `GET /api/notifications/failed` → `List<FailedEmailResponse>`, newest 100, `Channel = Email AND Status = Failed`. The recipient address is the guardian's `ContactEmail`, the address the Worker sends to.

## Database changes
None (no migration). One new read-only query on `Notifications`.

## How it was verified
- `dotnet build Klasly.slnx`: 0 warnings, 0 errors.
- `dotnet test Klasly.slnx`: **154 passed, 0 failed.** `ScanTests` ran 3 times in a row, all green.
- `dotnet list package`: every Microsoft package is 10.0.14 (plus `Microsoft.NET.Test.Sdk` 18.11.0, the test runner).
- `lan` profile: `dotnet run --launch-profile lan` logged `Now listening on: http://0.0.0.0:5201`. `curl http://192.168.0.114:5201/api/auth/me` from the PC returned the 401 problem-details.
- Failed emails page: checked in the browser pane as admin against the dev database. It lists the M12 "Brevo is not configured" rows (`admin-failed-emails.jpg`).

## Known limits / TODO
- **Real phone over Wi-Fi is not checked yet.** It needs the team to add the firewall rule and test on a phone (see HOW-TO-USE).
- Not checked in the browser: a teacher opening `/Admin/FailedEmails` being refused. The MVC `[Authorize(Roles = "Admin")]` is the same as every other Admin page, and the API's 403 is covered by the sweep.
- `/impeccable` pass on the new view not run. It copies the Terms page's table pattern.
- `./gradlew assembleDebug` not run: there was no Android change.
- The late cut-off is tested ±1 minute, not to the exact second (spec Q4: no `TimeProvider` refactor).
- Failed-emails list: no paging (newest 100); no retry or delete.
- On the `lan` profile, Swagger is reachable from the LAN (Development environment). That's acceptable on a hotspot for the demo.
- The spec's "MaxScore + 0.01 → 400" was already covered by `GradeTests` (20.01 of 20 refused), so no new test was added.
- Serilog is listed in CLAUDE.md but isn't installed. Flag it in the M15 review.
