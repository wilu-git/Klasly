# Milestone 3 — Web API authentication · Developer docs

## Summary
We asked for Milestone 3: login, a JWT with a role claim, password hashing, role policies, and OpenAPI + Swagger UI. The work followed `SPEC.md`, then `tasks/plan.md`, and was built task by task from `tasks/todo.md`, one commit per task.
`POST /api/auth/login` checks the password with ASP.NET Core Identity's `PasswordHasher<User>` and returns a 60-minute HS256 JWT carrying `sub`, `email`, `name` and `role`. `GET /api/auth/me` proves the token is accepted. Four role policies (`AdminOnly`, `TeacherOnly`, `StudentOnly`, `ParentOnly`) are ready for Milestone 4. Swagger UI runs at `/swagger` in Development only, with a Bearer **Authorize** button. The template `WeatherForecast` code is gone.

## Projects touched
- **WEB API** (`Klasly.API`): auth wiring, service, controller, Swagger UI
- **EF CORE** (`Klasly.Core` + `Klasly.Infrastructure`): DTOs, `IUserRepository`, `UserRepository`
- **MSSQL**: read only (no schema change)

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Core/DTOs/LoginRequest.cs` | added | Login body with `[Required]`, `[EmailAddress]`, `[MaxLength(256)]` |
| `src/Klasly.Core/DTOs/LoginResponse.cs` | added | Token, expiry (UTC), userId, fullName, role |
| `src/Klasly.Core/DTOs/CurrentUserResponse.cs` | added | Shape returned by `/api/auth/me` |
| `src/Klasly.Core/Interfaces/IUserRepository.cs` | added | `GetByEmailAsync(email, ct)` |
| `src/Klasly.Infrastructure/Repositories/UserRepository.cs` | added | EF Core lookup, `AsNoTracking` |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | Registers `IUserRepository` |
| `src/Klasly.API/Services/Interfaces/IAuthService.cs` | added | Login contract |
| `src/Klasly.API/Services/AuthService.cs` | added | Password check + token creation |
| `src/Klasly.API/Controllers/AuthController.cs` | added | `POST login`, `GET me` |
| `src/Klasly.API/Program.cs` | modified | JwtBearer, policies, ProblemDetails, OpenAPI Bearer transformer, Swagger UI; template removed |
| `src/Klasly.API/appsettings.json` | modified | `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiryMinutes` (no key) |
| `src/Klasly.API/Klasly.API.csproj` | modified | `JwtBearer` 10.0.14, `Swashbuckle.AspNetCore.SwaggerUI` 10.3.1, `UserSecretsId` |
| `src/Klasly.API/Klasly.API.http` | modified | Login and `/me` sample requests |
| `SPEC.md`, `tasks/plan.md`, `tasks/todo.md` | added | Spec, plan, task list for this milestone |

## Key code

**Startup refuses a missing or weak key** (`Program.cs`):
```csharp
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
    throw new InvalidOperationException("Jwt:Key is missing or shorter than 32 characters. Run: dotnet user-secrets set ...");
```

**Token validation** checks issuer, audience, lifetime and signing key. `ClockSkew` is 30 s instead of the default 5 minutes, so a 60-minute token doesn't quietly last 65. `MapInboundClaims = false` keeps the short claim names (`sub`, `role`), and `RoleClaimType = "role"` makes `RequireRole` work.

**Role policies**:
```csharp
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", p => p.RequireRole("Admin"))
    .AddPolicy("TeacherOnly", p => p.RequireRole("Teacher"))
    .AddPolicy("StudentOnly", p => p.RequireRole("Student"))
    .AddPolicy("ParentOnly", p => p.RequireRole("Parent"));
```
Usage from Milestone 4 on: `[Authorize(Policy = "AdminOnly")]`.

**Login rule** (`AuthService.LoginAsync`):
```csharp
var user = await users.GetByEmailAsync(request.Email.Trim(), ct);
if (user is null || !user.IsActive) return null;
var result = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
if (result == PasswordVerificationResult.Failed) return null;
return CreateToken(user);   // JsonWebTokenHandler, HS256, claims: sub, email, name, role
```
The controller turns `null` into `401` problem-details with `"Invalid email or password."`. It's the same message for an unknown email, a wrong password and an inactive account.

**Endpoints**
| Method | Route | Auth | Responses |
|---|---|---|---|
| POST | `/api/auth/login` | anonymous | 200 `LoginResponse` · 400 validation · 401 invalid credentials |
| GET | `/api/auth/me` | any valid JWT | 200 `CurrentUserResponse` · 401 |

## Database changes
None. Login uses the Milestone 2 seed users and their `Klasly@123` hash (Identity v3, PBKDF2-HMAC-SHA512, 100k iterations). `PasswordHasher<User>` verifies it as it is.

## How it was verified
- `dotnet build Klasly.slnx` → **0 errors, 0 warnings** after every task.
- `dotnet list Klasly.slnx package` → every Microsoft package is **10.0.14**. Swashbuckle UI is 10.3.1.
- Manual smoke test against LocalDB (`http://localhost:5201`):

| Check | Expected | Result |
|---|---|---|
| Start without `Jwt:Key` | clear startup error | ✅ names the `user-secrets` command |
| Admin login | 200, role Admin | ✅ |
| Student login (`aira.mendoza`) | 200, role Student | ✅ |
| Email in upper case | 200 | ✅ |
| Wrong password | 401 same message | ✅ |
| Unknown email | 401 same message | ✅ |
| Blank password | 400 validation | ✅ |
| Decoded token | sub/email/name/role/iss/aud/exp only; exp = now + 60 min | ✅ |
| `/me` no token / tampered / expired 2 min ago / wrong issuer | 401 | ✅ all four |
| `/me` valid token | 200 with matching claims | ✅ |
| `/swagger` in Development | 200, Authorize → `/me` works (checked in browser as a teacher) | ✅ |
| `/swagger` and `/openapi/v1.json` in Production | 404 | ✅ |

There are no automated tests yet. The spec chose to wait for Milestone 14.

## Known limits / TODO
- **Inactive account login was not tested end to end.** The rule is one `if`, but no seeded user has `IsActive = false`. Milestone 4's soft delete gives a real case to test.
- **Timing difference:** an unknown email returns slightly faster than a wrong password, because no hash runs. The message is identical, but timing could hint whether an email exists. Low risk for a campus app; it can be hardened in Milestone 14.
- No refresh tokens, lockout or rate limiting (out of scope in the spec).
- `dotnet ef` uses `Klasly.API` as its startup project, so it also needs `Jwt:Key` set before migrations will run.
- Role policies are registered but not yet used on any endpoint. Milestone 4 is the first real use.
- Serilog is still not added (CLAUDE.md §3.3). Device registration is deferred to Milestone 6.
