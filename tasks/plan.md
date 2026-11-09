# Implementation Plan: Milestone 3 — Web API authentication

> Built from [`SPEC.md`](../SPEC.md). Task details, acceptance criteria and verification steps are in [`todo.md`](./todo.md).
> Spec open questions are taken as their defaults: **60-minute token**, **no test project** (Milestone 14). If either is wrong, change the spec first, then this plan.

## Overview

Add login to `Klasly.API`. A user posts email + password and gets back a signed JWT carrying their role. Every later endpoint can then require a token, or a specific role, with one attribute. Swagger UI, Development only, lets the team try all of this in a browser. There are no database changes: the Milestone 2 seed users and their `Klasly@123` hash are used as they are.

## Dependency graph

```
Task 1  JWT + policies wiring (Program.cs, config, secret)
   │
   ├── Task 2  User lookup (Core interface → Infrastructure repository)
   │      │
   │      └── Task 3  Login endpoint (DTOs → AuthService → AuthController)
   │                │
   │                └── Task 4  /api/auth/me + .http samples
   │
   └── Task 5  Swagger UI + Bearer "Authorize" button
                    │
                    └── Task 6  Smoke test, reports, commit
```

Task 5 depends only on Task 1, so it could run alongside Tasks 2–4. All six are small and touch `Program.cs` or `Klasly.API.csproj`, so we build them **one after another** in a single session.

## Architecture decisions

| Decision | Why |
|---|---|
| Layer flow `AuthController → IAuthService → IUserRepository → EF Core` | Guide §2. The controller holds no logic, and the repository holds no rules. |
| `IUserRepository` in `Klasly.Core/Interfaces`, `UserRepository` in `Klasly.Infrastructure/Repositories` | CLAUDE.md §3.2. The Worker can reuse it later. |
| DTOs in `Klasly.Core/DTOs` | CLAUDE.md §3.2 and guide §1. Android will mirror these shapes. |
| `IAuthService` / `AuthService` in `Klasly.API/Services` | Guide §24. Business rules live in the API, not in Core. |
| `PasswordHasher<User>` from the ASP.NET Core shared framework | Guide §27.5. It reads the seeded Identity v3 hash, and needs no package. |
| `JsonWebTokenHandler` creates the token (not the older `JwtSecurityTokenHandler`) | The current .NET 10 API, and it comes with the JwtBearer package. |
| Token validation: issuer, audience, lifetime and signing key all checked; `ClockSkew` = 30 s | The default 5-minute skew would quietly stretch a 60-minute token. |
| Inbound claim mapping off (`MapInboundClaims = false`), `RoleClaimType = "role"`, `NameClaimType = "name"` | Claims keep their short JWT names (`sub`, `role`), so `/me` and policies read exactly what was issued. |
| Policies via `AddAuthorizationBuilder().AddPolicy("AdminOnly", p => p.RequireRole("Admin"))` and three more | Spec "role policies". The .NET 10 builder style. |
| Startup fails if `Jwt:Key` is missing or < 32 chars | HS256 needs ≥ 256-bit keys. A clear startup error beats a confusing 500 on first login. |
| `AddProblemDetails()`, and the controller returns `Problem(statusCode: 401, detail: ...)` | One error format, which CLAUDE.md §3.3 asks for. `[ApiController]` already gives 400 validation problem-details. |
| Swagger UI is mapped inside `if (IsDevelopment())` together with `MapOpenApi()` | CLAUDE.md §3.3. |
| Bearer scheme added with an OpenAPI **document transformer** | The .NET 10 way (Microsoft.OpenApi 2.x). It makes the Authorize button work. |

## Task list

### Phase 1: Foundation
- [ ] Task 1: JWT authentication + role policies wiring (XS–S)
- [ ] Task 2: User lookup repository (S)

### Checkpoint A — after Tasks 1–2
- [ ] `dotnet build Klasly.slnx` → 0 errors
- [ ] The API starts with the key set and fails clearly without it

### Phase 2: Core feature
- [ ] Task 3: `POST /api/auth/login` (M)
- [ ] Task 4: `GET /api/auth/me` + `.http` samples (S)

### Checkpoint B — after Tasks 3–4
- [ ] Smoke steps 1–6 from SPEC.md pass against LocalDB
- [ ] Review with the team before Swagger and reports

### Phase 3: Developer experience + wrap-up
- [ ] Task 5: Swagger UI + Bearer Authorize (S)
- [ ] Task 6: Full smoke test, package check, reports, commit (S)

### Checkpoint C — complete
- [ ] All SPEC.md success criteria checked
- [ ] `dotnet list package` shows no Microsoft package below 10.x
- [ ] Reports in `docs/reports/`, `INDEX.md` updated, committed to `dev`

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| The seeded hash doesn't verify with `PasswordHasher<User>` | High: nobody can log in | Task 3 tests a real seed login first. If it fails, stop and check the hash format (Identity v3, PBKDF2-SHA512 100k). Do **not** change the seed without asking, because that would need a migration. |
| `Jwt:Key` not set on a teammate's machine | Med: API won't start | Startup error message names the exact `dotnet user-secrets set` command. HOW-TO-USE.md repeats it. |
| Role claim not recognised, so policies always fail | Med | Set `RoleClaimType = "role"` explicitly. Task 4's `/me` shows the claims for proof. Real policy use is checked in Milestone 4. |
| Swashbuckle UI 10.x vs Microsoft.OpenApi 2.x API differences in the transformer | Low | Use only the OpenApi document transformer from `Microsoft.AspNetCore.OpenApi`. Swashbuckle UI just displays the JSON. |
| Database not created / LocalDB not running | Low | `dotnet ef database update` from Milestone 2. Covered in Troubleshooting. |

## Out of scope (from SPEC.md)

Refresh tokens, lockout/rate limiting, device registration, Serilog, CORS, test project, any MVC or Android login UI (Milestones 5 and 8).

## Open questions

None blocking. The token lifetime and the test project use the spec defaults (see top).
