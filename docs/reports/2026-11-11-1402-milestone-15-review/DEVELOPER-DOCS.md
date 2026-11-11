# Milestone 15 — Review of the whole solution; fix findings

## Summary

Milestone 15 was a review of the whole solution in a fixed order (Core + Infrastructure → API → Worker → Web → Android → tests), against one checklist:
- guide §25;
- the `CLAUDE.md` §3/§4 rules;
- the §27.5 security rules;
- §27.8 and §27.9, each traced to tests;
- a comparison of the migration script with `Klasly_Schema.sql`;
- the five review axes (correctness, readability, architecture, security, performance).

Every finding is in [`tasks/review-m15.md`](../../../tasks/review-m15.md) with an id, severity, `file:line`, rule and outcome:
- no Critical findings;
- **2 High**, both fixed: Serilog was missing, and deactivated accounts kept access until their token expired;
- **7 Medium**: 5 fixed, 1 deferred (login rate limit), 1 won't fix (the `lan` profile, accepted in M14);
- **11 Low**, logged (one fixed in passing);
- the 9 `shortcut:` limits were all kept.

A `security-auditor` agent also reviewed `Klasly.API`. I checked each of its findings against the code before adding it to the list.

## Projects touched

WEB API, EF CORE (Core + Infrastructure), CONSOLE WORKER, ANDROID KOTLIN, tests. WEB MVC was reviewed and not changed. MSSQL had no schema change.

## Files changed

| Path | Change | Why |
|---|---|---|
| `SPEC.md` | modified | M15 spec (approved), criteria ticked |
| `tasks/plan-m15.md`, `tasks/todo-m15.md` | added | M15 plan and tasks |
| `tasks/review-m15.md` | added | The findings list: baseline, schema comparison, findings, traceability tables |
| `src/Klasly.API/Klasly.API.csproj`, `src/Klasly.Worker/Klasly.Worker.csproj` | modified | F-01: Serilog packages |
| `src/Klasly.API/Program.cs` | modified | F-01: `AddSerilog`; F-02: `OnTokenValidated` refuses a token whose account is inactive |
| `src/Klasly.Worker/Program.cs` | modified | F-01: `AddSerilog` |
| `src/Klasly.API/appsettings*.json`, `src/Klasly.Worker/appsettings*.json` | modified | F-01: `Logging` → `Serilog:MinimumLevel` (same levels) |
| `src/Klasly.Core/Interfaces/IUserRepository.cs` | modified | F-02: `IsActiveAsync`; F-20: doc comment moved back onto `AddAsync` |
| `src/Klasly.Infrastructure/Repositories/UserRepository.cs` | modified | F-02: `IsActiveAsync` (one `AnyAsync`) |
| `src/Klasly.API/Services/AuthService.cs` | modified | F-03: hashes against a dummy hash for an unknown or inactive email |
| `android/KlaslyApp/app/src/main/res/xml/backup_rules.xml`, `data_extraction_rules.xml` | modified | F-06: leave `klasly_session.xml` out of backups and device transfers |
| `tests/Klasly.Tests/LoginTests.cs` | added | F-02, F-07: wrong password, deactivated login, an old token after deactivation |
| `tests/Klasly.Tests/AdminRulesTests.cs` | added | F-08: drop/re-enroll, soft-deleted account, no self-deactivation, one active term (runs alone) |
| `tests/Klasly.Tests/VerticalSliceTests.cs` | added | F-09: open → QR → register phone → scan → teacher's list, over HTTP |

## Key code

**F-02: a deactivated account loses access at once** (`Program.cs`):

```csharp
OnTokenValidated = async context =>
{
    var users = context.HttpContext.RequestServices.GetRequiredService<IUserRepository>();
    if (!int.TryParse(context.Principal?.FindFirst("sub")?.Value, out var userId)
        || !await users.IsActiveAsync(userId, context.HttpContext.RequestAborted))
        context.Fail("The account is deactivated.");
},
```

When the check fails, the existing `OnChallenge` handler answers with 401 "Session expired – please log in". MVC turns that into "Your session ended, please log in again", and Android logs the user out with the message. The cost is one indexed `EXISTS` query per authenticated API call.

**F-03: login timing** (`AuthService.cs`):

```csharp
var result = hasher.VerifyHashedPassword(user ?? TimingDummy, user?.PasswordHash ?? DummyHash, request.Password);
if (user is not { IsActive: true } || result == PasswordVerificationResult.Failed) return null;
```

**F-01: Serilog** (both hosts):

```csharp
builder.Services.AddSerilog((_, logger) => logger.ReadFrom.Configuration(builder.Configuration).WriteTo.Console());
```

```json
"Serilog": { "MinimumLevel": { "Default": "Information", "Override": { "Microsoft.AspNetCore": "Warning" } } }
```

The setup uses a logger per host, not the static `Log.Logger`, so `WebApplicationFactory` tests can start the API again and again. There is no `UseSerilogRequestLogging`, so request bodies and headers (passwords, JWTs) are never logged.

**New packages** (latest stable that supports `net10.0`):

| Package | Version | Where |
|---|---|---|
| `Serilog.AspNetCore` | 10.0.0 | API |
| `Serilog.Extensions.Hosting` | 10.0.0 | Worker |
| `Serilog.Settings.Configuration` | 10.0.1 | Worker |
| `Serilog.Sinks.Console` | 6.1.1 | Worker |

The Worker doesn't use `Serilog.AspNetCore`, because that package would pull the ASP.NET Core framework into a console worker.

## Database changes

None. The only migration is still `InitialCreate`, and its script matches `Klasly_Schema.sql` on every §27.7 convention. Details are in `review-m15.md`.

## How it was verified

| Command | Result |
|---|---|
| `dotnet build Klasly.slnx` | 0 warnings, 0 errors (baseline 0) |
| `dotnet test Klasly.slnx` | **162 / 162** passed, run twice (baseline 154; 8 new tests) |
| New F-02 test before the fix | failed: `Expected: Unauthorized, Actual: OK` |
| `./gradlew assembleDebug testDebugUnitTest lintDebug` (`JAVA_HOME` = `D:\Android Studio\jbr`) | BUILD SUCCESSFUL, 11 / 11 unit tests, lint 17 warnings / 0 errors (unchanged) |
| `dotnet list package` (+ `--include-transitive`) | Every `Microsoft.AspNetCore.*` / `EntityFrameworkCore.*` / `Extensions.*` package is 10.x |
| `dotnet list package --vulnerable --include-transitive` / `--deprecated` | none |
| Secrets scan of tracked files and `git log -p --all` | clean (placeholders only) |
| Manual: API on `http` profile, student login + a wrong-password login | Serilog output; the log holds no `Klasly@123`, no `eyJ` (JWT) and no email |
| Manual: Worker (Production env, `Klasly_Tests`, cron every minute, no Brevo key) | One tick logged all 4 summary lines; no guardian email or api-key in the log |

## Known limits / TODO

- **Task 12 (team):** the real-phone end-to-end run over Wi-Fi with screenshots. This is the only open M15 criterion, and it also closes the open M14 criterion. Steps are in `HOW-TO-USE.md`.
- **F-04 (deferred):** no rate limit on login. A limit per email needs the request body read before model binding, and a limit per IP would throttle the MVC server. The way forward is a failed-attempt counter + lockout on `Users`, which is a schema change, so ask first.
- **F-05 (won't fix):** the `lan` profile runs in Development (Swagger) over HTTP. Use it only on the team's own hotspot.
- **F-10 (deferred):** one phone can be registered to several students. A filtered unique index would fix it, but that is a schema change.
- **Low items logged** (F-11 to F-19). Two of them are:
  - `dotnet tool update --global dotnet-ef` (the tool is 10.0.11, the runtime 10.0.14);
  - 17 Android lint warnings (unused colors, newer dependency versions).
- The deprecated Android APIs (`security-crypto`, FCM `getToken` / `onNewToken`) stay until after the demo (spec Q3).
