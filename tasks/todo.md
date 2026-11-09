# Milestone 3 — Task list

> Plan: [`plan.md`](./plan.md) · Spec: [`SPEC.md`](../SPEC.md)
> Build command for every task: `dotnet build Klasly.slnx`. No test project yet, so verification is by build plus a manual check.

---

## Task 1: JWT authentication + role policies wiring

**Description:** Give the API what it needs to trust tokens: a JWT key stored in user-secrets, issuer/audience/expiry in config, JwtBearer authentication, the four role policies and problem-details errors. Delete the template `WeatherForecast` code. No endpoints yet.

**Acceptance criteria:**
- [ ] `Klasly.API.csproj` has `Microsoft.AspNetCore.Authentication.JwtBearer` 10.0.14 and a `UserSecretsId`. `appsettings.json` has `Jwt:Issuer`, `Jwt:Audience` and `Jwt:ExpiryMinutes = 60`, and no key.
- [ ] `Program.cs` registers JwtBearer (issuer, audience, lifetime and key checks; `ClockSkew` 30 s; `MapInboundClaims = false`; `role`/`name` claim types), the policies `AdminOnly`, `TeacherOnly`, `StudentOnly`, `ParentOnly`, `AddProblemDetails()`, `UseAuthentication()` / `UseAuthorization()`, and `MapControllers()`.
- [ ] Startup throws a clear error if `Jwt:Key` is missing or shorter than 32 characters.

**Verification:**
- [ ] Build succeeds
- [ ] Manual: `dotnet run` with the key set → starts. With the key removed → fails with the message that names the `user-secrets` command.

**Dependencies:** None

**Files likely touched:**
- `src/Klasly.API/Klasly.API.csproj`
- `src/Klasly.API/Program.cs`
- `src/Klasly.API/appsettings.json`

**Estimated scope:** S (3 files)

---

## Task 2: User lookup repository

**Description:** Add the data-access piece login needs: find a user by email. The interface goes in Core and the EF Core implementation in Infrastructure, registered by `AddInfrastructure()`.

**Acceptance criteria:**
- [ ] `IUserRepository.GetByEmailAsync(string email, CancellationToken ct)` returns `Task<User?>`.
- [ ] `UserRepository` uses `AsNoTracking()` and `FirstOrDefaultAsync` on `Users` by `Email`. It applies no business rules (the `IsActive` check belongs in the service).
- [ ] `AddInfrastructure()` registers `IUserRepository → UserRepository` as scoped.

**Verification:**
- [ ] Build succeeds
- [ ] Manual: none on its own. Checked by Task 3's login.

**Dependencies:** None (it can be done before or after Task 1)

**Files likely touched:**
- `src/Klasly.Core/Interfaces/IUserRepository.cs` (new)
- `src/Klasly.Infrastructure/Repositories/UserRepository.cs` (new)
- `src/Klasly.Infrastructure/DependencyInjection.cs`

**Estimated scope:** S (3 files)

---

## Checkpoint A — after Tasks 1–2
- [ ] `dotnet build Klasly.slnx` → 0 errors, no new warnings
- [ ] The API starts with the key and fails clearly without it

---

## Task 3: `POST /api/auth/login`

**Description:** The main feature. Check the password with `PasswordHasher<User>`, refuse deactivated accounts, and issue the JWT (`sub`, `email`, `name`, `role`, `iss`, `aud`, `exp`). Every failure returns the same 401.

**Acceptance criteria:**
- [ ] A seed admin login with `Klasly@123` → `200` with `token`, `expiresAtUtc` (UTC, now + 60 min), `userId`, `fullName`, `role`.
- [ ] Wrong password, unknown email or `IsActive = false` → `401` problem-details, `detail: "Invalid email or password."` (same for all three). Blank or badly formatted fields → `400`.
- [ ] Nothing sensitive in the token or the response (no hash, device ID or FCM token). Passwords and tokens are never logged.

**Verification:**
- [ ] Build succeeds
- [ ] Manual (API running, LocalDB seeded): admin login 200; student `aira.mendoza@klasly.test` login 200 with `role = Student`; wrong password 401; unknown email 401; blank password 400
- [ ] Manual: decode the test token at jwt.io and confirm the claims

**Dependencies:** Tasks 1, 2

**Files likely touched:**
- `src/Klasly.Core/DTOs/LoginRequest.cs` (new)
- `src/Klasly.Core/DTOs/LoginResponse.cs` (new)
- `src/Klasly.API/Services/Interfaces/IAuthService.cs` (new)
- `src/Klasly.API/Services/AuthService.cs` (new)
- `src/Klasly.API/Controllers/AuthController.cs` (new) + DI lines in `Program.cs`

**Estimated scope:** M (5 files + 2 DI lines)

---

## Task 4: `GET /api/auth/me` + `.http` samples

**Description:** A protected endpoint that returns the caller's claims, proving tokens are checked end to end. Replace the template request in `Klasly.API.http` with login and `/me` samples.

**Acceptance criteria:**
- [ ] `GET /api/auth/me` has `[Authorize]` and returns `{ userId, email, fullName, role }` read from the token's claims.
- [ ] No token, a tampered token or an expired token → `401`.
- [ ] `Klasly.API.http` has: admin login, student login, wrong password, blank password, `/me` with no token, and `/me` with a token captured from the login response.

**Verification:**
- [ ] Build succeeds
- [ ] Manual: run every request in `Klasly.API.http` and get the expected status codes

**Dependencies:** Task 3

**Files likely touched:**
- `src/Klasly.API/Controllers/AuthController.cs`
- `src/Klasly.API/Klasly.API.http`

**Estimated scope:** S (2 files)

---

## Checkpoint B — after Tasks 3–4
- [ ] Smoke steps 1–6 from SPEC.md pass
- [ ] Team review before continuing

---

## Task 5: Swagger UI + Bearer Authorize

**Description:** Show the OpenAPI document in Swagger UI at `/swagger`, in Development only, and add a Bearer security scheme so the Authorize button lets us call `/me`.

**Acceptance criteria:**
- [ ] `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1 added. `UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "Klasly API v1"))` sits inside the `IsDevelopment()` block.
- [ ] An OpenAPI document transformer adds a `Bearer` (HTTP, JWT) security scheme, so the Authorize button appears.
- [ ] In Production (`ASPNETCORE_ENVIRONMENT=Production`), `/swagger` and `/openapi/v1.json` return 404.

**Verification:**
- [ ] Build succeeds
- [ ] Manual: open `https://localhost:7256/swagger`, run login, Authorize with the token, run `/me` → 200

**Dependencies:** Task 1 (it works best after Task 4, which gives `/me` to try)

**Files likely touched:**
- `src/Klasly.API/Klasly.API.csproj`
- `src/Klasly.API/Program.cs`

**Estimated scope:** S (2 files)

---

## Task 6: Full smoke test, package check, reports, commit

**Description:** Run the full SPEC.md checklist, confirm package versions, write the three reports required by CLAUDE.md §5, mark the spec Approved/Done, and commit to `dev`.

**Acceptance criteria:**
- [ ] All 7 SPEC.md smoke steps pass. `dotnet list Klasly.slnx package` shows no Microsoft package below 10.x.
- [ ] `docs/reports/<yyyy-MM-dd-HHmm>-milestone-3-auth/` has `DEVELOPER-DOCS.md`, `HOW-TO-USE.md` and `FLOWCHARTS.md`. `docs/reports/INDEX.md` has a new line.
- [ ] One commit on `dev`, with no secrets in the diff.

**Verification:**
- [ ] `git diff --cached` checked for keys before committing
- [ ] Build succeeds

**Dependencies:** Tasks 1–5

**Files likely touched:**
- `docs/reports/<folder>/*.md` (3 new)
- `docs/reports/INDEX.md`
- `SPEC.md`, `tasks/todo.md` (status ticks)

**Estimated scope:** S

---

## Checkpoint C — complete
- [ ] Every SPEC.md success criterion ticked
- [ ] Ready for `/review` and for Milestone 4 (admin CRUD uses `AdminOnly`)
