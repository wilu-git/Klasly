# Spec: Milestone 3 — Web API authentication

> Status: **Draft — waiting for team approval.** Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec.

## Assumptions (correct these before we build)

1. Token lifetime is **60 minutes**, set in config (`Jwt:ExpiryMinutes`). There are no refresh tokens (no document asks for them), so clients log in again after it expires.
2. **No test project in this milestone.** Testing is by hand with `Klasly.API.http` and Swagger UI. Automated role-access tests come in Milestone 14 (CLAUDE.md §6).
3. Login uses **email + password**. Email is unique in `Users` (guide §27.6). Email matching is case-insensitive, which is how SQL Server's default collation already compares text.
4. Any account with `IsActive = false` (soft-deleted) cannot log in.
5. Device registration (`RegisteredDeviceId`) is **not** part of this milestone. No document says how a device gets registered, so it will be raised at Milestone 6 (scan check #6).
6. Serilog is **not** part of this milestone. It will be added once for the whole API later.

## Objective

Every Klasly client needs to know **who** is calling the Web API and **what role** they have. The MVC portal (Admin, Teacher, Parent) and the Android app (Student, Parent) both log in through the Web API (guide §11, §27.2). This milestone builds that front door:

- A user exchanges email + password for a signed JWT that carries their role.
- The API trusts only tokens it signed itself, and only until they expire.
- Later milestones can lock any endpoint to a role with one attribute.
- Developers can try every endpoint in Swagger UI, including the ones that need a token.

### User stories
- As **any user**, I log in with my email and password and get a token, so the app can call the API for me.
- As **any user**, if I type a wrong password I see "Invalid email or password." The message never reveals whether the email exists.
- As a **developer**, I open `/swagger`, log in, click **Authorize**, paste the token, and call protected endpoints.

## Tech stack

| Item | Version | Why |
|---|---|---|
| .NET / ASP.NET Core | 10 (`net10.0`) | CLAUDE.md §4.1 |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | 10.0.14 | Checks the JWT on incoming requests. **New package.** |
| `Swashbuckle.AspNetCore.SwaggerUI` | 10.3.1 | Swagger UI page pointed at `/openapi/v1.json` (CLAUDE.md §3.3). **New package.** |
| `Microsoft.AspNetCore.OpenApi` | 10.0.14 (already installed) | Builds the OpenAPI document |
| `PasswordHasher<User>` (ASP.NET Core Identity) | ships with ASP.NET Core 10 | Password hashing (guide §27.5). No package needed. |
| `JsonWebTokenHandler` | comes with the JwtBearer package | Creates the token |

## API contract

### `POST /api/auth/login` — anonymous
Request:
```json
{ "email": "admin@klasly.test", "password": "Klasly@123" }
```
`200 OK`:
```json
{
  "token": "eyJhbGciOi...",
  "expiresAtUtc": "2026-11-09T10:00:00Z",
  "userId": 1,
  "fullName": "Klasly Administrator",
  "role": "Admin"
}
```
| Case | Response |
|---|---|
| Email or password missing/blank, or email badly formatted | `400` validation problem-details |
| Unknown email, wrong password, or `IsActive = false` | `401` problem-details, `detail: "Invalid email or password."` (the same message for all three) |

### `GET /api/auth/me` — any valid token
`200 OK`: `{ "userId": 1, "email": "...", "fullName": "...", "role": "Admin" }`, read from the token's claims.
No token, a bad token, or an expired token → `401`.

### JWT contents
- Algorithm HS256, signed with `Jwt:Key`, which must be at least 32 characters. It lives in **user-secrets** only.
- `iss` = `Jwt:Issuer`, `aud` = `Jwt:Audience`, `exp` = now + `Jwt:ExpiryMinutes` (UTC).
- Claims: `sub` (UserId), `email`, `name` (FullName), `role` (`Admin` | `Teacher` | `Student` | `Parent`).
- The password hash, device ID and FCM token are **never** put in the token.

### Role policies (for later milestones)
`AdminOnly`, `TeacherOnly`, `StudentOnly`, `ParentOnly`, each requiring that role claim. Usage: `[Authorize(Policy = "TeacherOnly")]`.

## Commands

```bash
# one-time: give the API its secrets store and a JWT key (do not commit the key)
dotnet user-secrets init --project src/Klasly.API
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>" --project src/Klasly.API

# build + package check
dotnet build Klasly.slnx
dotnet list Klasly.slnx package

# run (Swagger UI at https://localhost:7256/swagger)
dotnet run --project src/Klasly.API --launch-profile https
```

Database: no migration. The seeded users and their `Klasly@123` hash (from Milestone 2) are used as they are.

## Project structure (files this milestone touches)

```
src/Klasly.Core/
  DTOs/LoginRequest.cs              (new) email + password, with [Required]/[EmailAddress]
  DTOs/LoginResponse.cs             (new) token, expiresAtUtc, userId, fullName, role
  Interfaces/IUserRepository.cs     (new) GetByEmailAsync(email, ct)
src/Klasly.Infrastructure/
  Repositories/UserRepository.cs    (new) EF Core, AsNoTracking
  DependencyInjection.cs            (modified) register IUserRepository
src/Klasly.API/
  Services/Interfaces/IAuthService.cs  (new)
  Services/AuthService.cs              (new) verify password, create token
  Controllers/AuthController.cs        (new) login + me
  Program.cs                           (modified) auth, policies, ProblemDetails, OpenAPI + Swagger UI; template WeatherForecast removed
  appsettings.json                     (modified) Jwt:Issuer / Audience / ExpiryMinutes (no key)
  Klasly.API.csproj                    (modified) 2 packages + UserSecretsId
  Klasly.API.http                      (modified) login + me sample requests
```

Layer flow (guide §2): `AuthController → IAuthService → IUserRepository → EF Core → SQL Server`.

## Code style

File-scoped namespaces, primary constructors, `async` with `CancellationToken` all the way down, DTOs out (never entities), and settings read from config. Example of the intended shape:

```csharp
namespace Klasly.API.Services;

public class AuthService(IUserRepository users, IPasswordHasher<User> hasher, IConfiguration config) : IAuthService
{
    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email, ct);
        if (user is null || !user.IsActive) return null;

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        return CreateToken(user);   // null → controller returns 401 "Invalid email or password."
    }
}
```

## Testing strategy

- **Build:** `dotnet build` with 0 errors. `dotnet list package` shows no Microsoft package below 10.x.
- **Manual smoke test** (API running against LocalDB with the Milestone 2 seed), using `Klasly.API.http`:
  1. Admin login → `200`, `role = Admin`.
  2. Student login (`aira.mendoza@klasly.test`) → `200`, `role = Student`.
  3. Wrong password → `401` "Invalid email or password."
  4. Unknown email → `401`, same message.
  5. Blank password → `400`.
  6. `GET /api/auth/me` with no token → `401`; with the admin token → `200` with matching claims.
  7. `/swagger` loads in Development, and Authorize + `/me` works.
- No automated tests in this milestone (assumption 2).

## Boundaries

- **Always:** keep `Jwt:Key` in user-secrets; give the same 401 message for every login failure; use UTC times; follow Controller → Service → Repository; run `dotnet build` before reporting; write the three reports in `docs/reports/`.
- **Ask first:** adding a test project; adding refresh tokens, account lockout, or rate limiting; any schema change or migration; any package beyond the two listed.
- **Never:** commit a JWT key or put it in `appsettings*.json`; log passwords or tokens (guide §16); return the `User` entity or `PasswordHash`; turn on CORS (CLAUDE.md §3.3); expose Swagger outside Development.

## Success criteria

- [ ] All 7 smoke-test steps above give the expected status codes.
- [ ] A token decoded at jwt.io (test token only) shows `sub`, `email`, `name`, `role`, `iss`, `aud`, `exp`, and nothing sensitive.
- [ ] The API refuses to start with a clear error if `Jwt:Key` is missing or shorter than 32 characters.
- [ ] Four role policies are registered and ready for Milestone 4.
- [ ] `WeatherForecast` template code is gone.
- [ ] Reports written; `docs/reports/INDEX.md` updated.

## Open questions

1. Is a **60-minute** token lifetime OK, or do you want longer (for example 8 hours, so students aren't logged out during a long day)?
2. Should we add an xUnit **test project** now, or wait for Milestone 14?
