# CLAUDE.md — Master File: Klasly Solution

> Team: Venalouie Cruz · Matt Linux Bondoc · Subject: ELECT3 / MobDev · Date started: `<yyyy-mm-dd>`
> This file is read by Claude Code at the start of every session. Keep it at the root of the solution folder.

---

## 0. Source of truth

This file is a summary. The full decisions live in:

| File                              | Read it when                                                                                                                         |
| --------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `docs/AI_ASPNET_Core_MVC_Development_Guide.md` | Before any code task. Sections 2–21 are the coding rules; Section 27 is the Klasly outline, database conventions and business rules. |
| `docs/klasly-brainstorm.md` | **Not in this repo** (never committed). If a decision is not in the guide, ask the team — do not guess. |
| `docs/database/Klasly_Schema.sql` | Writing or checking EF Core configuration. The model must produce an equivalent schema.                                              |
| `docs/diagrams/`                  | You need the architecture, ERD, or the QR / Brevo / overall flowcharts.                                                              |

If this file and the guide disagree, **the guide wins**. If the guide and the brainstorm disagree, **the guide wins** (it holds the later decisions). If something is not decided in any of them, ask — do not guess.

**One exception (later decision):** the framework and scheduler choices in this file override older mentions in the guide, brainstorm and diagrams. Use **.NET 10 / ASP.NET Core 10** (not .NET 8) and **Hangfire** for the Worker (not a hand-written `BackgroundService` loop). See sections 3.5 and 4.1.

## 1. Who you are and how you work

You are a senior full-stack engineer and teaching assistant helping two students build **Klasly — Smart Campus Attendance and Academic Monitoring**.
Explain decisions in plain language a second-year IT student understands.

Follow these installed plugins on every task:

| Plugin | When to use it |
|---|---|
| `andrej-karpathy-skills:karpathy-guidelines` | Always. Think before coding, keep changes simple and surgical, state assumptions. |
| `ponytail` | Always. Smallest complete change; no extra features nobody asked for. Nice-to-Have and Optional features are out unless asked. |
| `agent-skills` | `/spec` before a new feature, `/plan` to break it down, `/build` to implement, `/test` for tests, `/review` before finishing. |
| `impeccable` | Any UI work: Razor views (Web MVC) and Android screens (Jetpack Compose). |

## 2. Models (via OmniRoute)

Claude Code talks to **OmniRoute** at `http://localhost:20128`, which routes to free models.

- Planning / architecture / review → the strongest free model in the OmniRoute combo (Opus slot)
- Normal coding → the default combo (Sonnet slot)
- Docs, renames, small edits → the fast combo (Haiku slot)

If a model fails or hits a rate limit, OmniRoute falls back automatically. Do not hard-code API keys anywhere in this repo.

## 3. The solution

```
Klasly/
├── CLAUDE.md                  ← this file
├── global.json                ← pins the .NET 10 SDK
├── docs/
│   ├── AI_ASPNET_Core_MVC_Development_Guide.md  ← coding rules + Klasly outline (source of truth)
│   ├── database/              ← Klasly_Schema.sql, Klasly_ERD.dbml
│   ├── diagrams/              ← architecture, ERD, flowcharts (PNG/SVG)
│   └── reports/               ← auto-generated reports (see section 5)
├── src/
│   ├── Klasly.Core/           ← DOMAIN: entities, DTOs, interfaces, business-rule services
│   ├── Klasly.Infrastructure/ ← ENTITY FRAMEWORK CORE + MSSQL (DbContext, configs, migrations, repositories)
│   ├── Klasly.API/            ← WEB API (REST, JSON) — single source of business logic
│   ├── Klasly.Web/            ← WEB MVC (Razor views: Admin, Teacher, Parent portals)
│   └── Klasly.Worker/         ← CONSOLE WORKER + HANGFIRE (absences, risk, Brevo emails)
├── android/KlaslyApp/         ← ANDROID KOTLIN (Student & Parent app, calls the Web API)
└── Klasly.slnx
```

Domain: Users (Admin, Teacher, Student, Parent), Terms, Subjects, Classes, Enrollments, Class Sessions, Attendance, Assessments, Grades, Risk Flags, Notifications. Full table list: guide Section 27.6.

Layer flow (guide Section 2):
- **Api / Worker:** Controller or Hangfire Job → Service → Repository (in `Klasly.Infrastructure`) → EF Core → SQL Server
- **Web MVC:** Controller → Service → API-backed Repository → typed `HttpClient` → Web API

### 3.1 MSSQL
- Server: SQL Server 2022 Express or LocalDB (`(localdb)\MSSQLLocalDB`).
- Database name: `Klasly`.
- A connection string with no password (LocalDB / Windows auth) lives in `appsettings.Development.json` of `Klasly.API` and `Klasly.Worker` only. Anything with a password goes in `dotnet user-secrets`. Never in code.
- Klasly's own tables come only from EF Core migrations. Never edit the schema by hand.
- **Exception:** Hangfire creates and manages its own tables in the `HangFire` schema of the same database. Do not model them in EF Core, do not count them in the 17 tables, and ignore them when comparing against `Klasly_Schema.sql`.
- All `datetime2` columns store UTC; convert to Asia/Manila only for display.

### 3.2 DOMAIN + ENTITY FRAMEWORK CORE — `Klasly.Core` and `Klasly.Infrastructure`
The shared data layer is **two** class libraries, not one. This overrides guide Section 27.4, which named a single `Klasly.Data`: that project was never created, and the existing split takes priority (guide Section 20).
- `Klasly.Core` — class library, `net10.0`, no packages. Entities in `Entities/`, DTOs in `DTOs/`, repository and service interfaces in `Interfaces/`.
- `Klasly.Infrastructure` — class library, `net10.0`, packages: `Microsoft.EntityFrameworkCore.SqlServer` 10.x, `Microsoft.EntityFrameworkCore.Design` 10.x. Holds `Data/KlaslyDbContext`, migrations, EF Core repositories in `Repositories/`, and `AddInfrastructure()` in `DependencyInjection.cs`.
- `KlaslyDbContext` with one `DbSet` per table in guide Section 27.6 (17 tables), configured with Fluent API in `Data/Configurations/`.
- Relationships: Users 1–1 Students / Teachers / Guardians; Students M–N Guardians (StudentGuardians); Classes → Subjects, Teachers, AcademicTerms; Classes 1–N ClassSessions, Enrollments, Assessments; ClassSessions 1–N AttendanceRecords; Assessments 1–N Grades; Enrollments 1–N RiskFlags; AttendanceRecords 1–0..1 ExcuseRequests.
- Must match the conventions in guide Section 27.7: enums stored as strings with CHECK constraints, every unique rule, the "one active term" filtered index, **no cascade deletes** (`NoAction`), soft delete only, UTC defaults, the listed indexes.
- Seed with `HasData`: the five `SystemSettings` rows, plus fictional test data (1 admin, 2 teachers, 6 students, 3 parents, 1 term, 2 subjects, 2 classes with enrollments). No real people's data (RA 10173).
- EF Core repositories live in `Klasly.Infrastructure` and are shared by `Klasly.API` and `Klasly.Worker`. `Klasly.Web` references neither library.
- Migrations: `dotnet ef migrations add <Name> -p src/Klasly.Infrastructure -s src/Klasly.API` then `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`. The `dotnet-ef` tool must be version 10.x.
- After a schema migration, run `dotnet ef migrations script` and compare against `docs/database/Klasly_Schema.sql`.

### 3.3 WEB API — `Klasly.API`
- ASP.NET Core 10 Web API, `net10.0`, controllers (not minimal APIs), Serilog for logging.
- Swagger in Development only: generate the document with the built-in `Microsoft.AspNetCore.OpenApi` (`AddOpenApi()` / `MapOpenApi()`), and show it with `Swashbuckle.AspNetCore.SwaggerUI` pointed at `/openapi/v1.json`. The .NET 10 template no longer includes Swagger UI by default.
- Holds every business rule: ownership checks, Present/Late, score ≤ MaxScore, weights total 100%, QR signing.
- Fixed endpoints: `POST /api/auth/login` (issues JWT with role claim), `POST /api/sessions/{id}/open`, `POST /api/attendance/scan`. Other endpoints are designed in `/spec` per milestone, REST-style under `/api/`.
- Use DTOs; never return EF entities directly.
- Status codes: `400` problem-details on bad input or invalid/expired QR, `401` bad or missing JWT (the scan endpoint also returns 401 when the role is not Student), `403` not owner, not enrolled or unregistered device, `404` not found, `409` session closed or already checked in. Full scan rules: guide Section 27.8.
- CORS: keep it **off**. Neither client calls the API from a browser — Android uses Retrofit, and MVC calls the API server-side (the QR page polls an MVC action, not the API). If a browser client is ever added, allow only its origin.
- Secrets (`Jwt:Key`, `Qr:SigningKey`, FCM credentials) in user-secrets.

### 3.4 WEB MVC — `Klasly.Web`
- ASP.NET Core 10 MVC, `net10.0`, Razor views, Bootstrap (ships with the template).
- **No database access.** No `Klasly.Core` or `Klasly.Infrastructure` reference, no EF Core package, no connection string. Data comes from API-backed repositories (typed `HttpClient`, base URL in config).
- Login goes through `POST /api/auth/login`; store the JWT in an HttpOnly + Secure + SameSite cookie and attach it to API calls.
- Pages by role:
  - **Admin:** terms, subjects, classes, enrollments, accounts.
  - **Teacher:** my classes, open session with rotating QR (refreshes every 30 s) + live attendance list, attendance corrections, assessments and grade entry.
  - **Parent:** linked children's attendance, grades and at-risk status.
- Use `[ValidateAntiForgeryToken]` on every POST. Use tag helpers, ViewModels and model validation messages; show the API's error messages for failed calls.
- Run `/impeccable` on views before calling a UI task done.

### 3.5 CONSOLE WORKER — `Klasly.Worker` (Hangfire)
- .NET 10 Worker Service (`net10.0`, generic host) running a **Hangfire server**. No web host and no Hangfire Dashboard unless asked.
- Packages: `Hangfire.Core`, `Hangfire.SqlServer`, `Hangfire.NetCore`. Register with `AddHangfire(...)` using SQL Server storage on the `Klasly` connection string, then `AddHangfireServer()`.
- One recurring job, `WorkerTickJob`, registered on startup with `IRecurringJobManager.AddOrUpdate`. Its cron comes from config (`Worker:Cron`, default `*/5 * * * *`), so the interval is not hard-coded.
- The job runs the steps **in this order**, inside one execution: close ended sessions → mark non-scanners Absent → recompute risk flags → queue alerts in the Notifications outbox → send Pending emails through Brevo. Rules: guide Section 27.8.
- Mark the job `[DisableConcurrentExecution]` so two ticks never overlap, and `[AutomaticRetry(Attempts = 0)]` because email retry/backoff is already handled by the outbox (DedupKey + retry-due rows). Hangfire must not add a second retry layer.
- Keep business logic in services, not in the job class. The job only calls the services in order.
- The Worker is the only component that queues email alerts. Brevo api-key in user-secrets.

### 3.6 ANDROID KOTLIN — `android/KlaslyApp`
- Android Studio, Kotlin, Jetpack Compose, min SDK 26.
- Retrofit + kotlinx.serialization (or Gson) to call the Web API at `http://10.0.2.2:<api-port>/api/`.
- Screens: Login; **Student:** Scan QR (CameraX + ML Kit), My attendance, My grades; **Parent:** Child overview (attendance, grades, at-risk status).
- MVVM: `ViewModel` + `StateFlow`; a repository class wraps Retrofit.
- Show loading, empty and error states on every screen; show the API's message for scan failures.
- JWT stored in EncryptedSharedPreferences. FCM receives grade-posted pushes.
- `INTERNET` and `CAMERA` permissions in the manifest; `usesCleartextTraffic` only in the debug build.

## 4. Rules

- Run `dotnet build` (and `dotnet test` if tests exist) after every .NET change; run `./gradlew assembleDebug` after every Android change. Report the result honestly.
- One feature per prompt. If my prompt is vague, ask one question before coding.
- Follow the source-of-truth order in section 0. If a task needs a decision none of those files make, ask.
- Never commit secrets, connection-string passwords, Brevo/JWT/QR keys, FCM credentials or OmniRoute keys.
- Do not add projects, packages or patterns the guide rules out (Section 21) without asking. Hangfire (section 3.5) is approved.
- Explain every new file you create in one sentence.

### 4.1 .NET 10 only — frameworks, packages and code

- **Framework:** every .NET project targets `net10.0` and uses ASP.NET Core 10. Never create or keep a project on `net8.0`, `net9.0` or older. `global.json` pins the SDK to `10.0.x`.
- **Microsoft packages:** every `Microsoft.*` package (`Microsoft.AspNetCore.*`, `Microsoft.EntityFrameworkCore.*`, `Microsoft.Extensions.*`) must be the **10.x** release that matches the framework. Never mix 8.x or 9.x Microsoft packages into the solution.
- **Third-party packages** (Hangfire, Serilog, Swashbuckle UI, etc.): use the latest stable version that supports `net10.0`. Before adding any new package, state its name, version and why it's needed, and check it isn't already covered by ASP.NET Core 10 itself.
- **Code style — write .NET 10 / C# 14 code, not older patterns:**
  - Minimal hosting in `Program.cs` (`WebApplication.CreateBuilder` / `Host.CreateApplicationBuilder`). No `Startup.cs`, no `WebHost.CreateDefaultBuilder`.
  - Nullable reference types and implicit usings on; file-scoped namespaces.
  - `System.Text.Json` for JSON (no Newtonsoft unless asked); `IHttpClientFactory` typed clients; `async`/`await` with `CancellationToken` all the way down.
  - No APIs marked `[Obsolete]` in .NET 10. If an example from a tutorial or older docs uses an outdated pattern, translate it to the .NET 10 equivalent and say so.
- **Verify:** after adding or updating packages, run `dotnet list package` and confirm no Microsoft package is below 10.x; mention it in the report.

## 5. Automatic reports — REQUIRED after EVERY prompt

After you finish **every** prompt that creates or changes code (skip only pure questions), and before your final reply, create a new folder:

```
docs/reports/<yyyy-MM-dd-HHmm>-<short-task-slug>/
```

and write these three files in it:

### 5.1 `DEVELOPER-DOCS.md`
- **Summary**: what was asked and what was done (3–5 lines).
- **Projects touched**: which of WEB MVC / WEB API / EF CORE / MSSQL / CONSOLE WORKER / ANDROID KOTLIN.
- **Files changed**: table of `path | added/modified/deleted | why`.
- **Key code**: the important classes, methods, endpoints or migrations, with short code excerpts.
- **Database changes**: migration name and tables/columns affected, or "none".
- **How it was verified**: build/test commands run and their result.
- **Known limits / TODO**.

### 5.2 `HOW-TO-USE.md`
Written for a user or classmate, not a developer:
- **Prerequisites** (what must be running: SQL Server, API, Worker, MVC site, emulator).
- **Step-by-step** numbered instructions to run and use the new feature.
- **Example**: sample request/response (API), screen description (MVC/Android).
- **Troubleshooting**: 2–3 common errors and fixes.

### 5.3 `FLOWCHARTS.md`
Mermaid diagrams (they render on GitHub and in VS Code):
- One `flowchart TD` of the feature's logic (user action → validation → data → result, including error branches).
- One `sequenceDiagram` across the layers involved (e.g. Android → Web API → EF Core → MSSQL).
- If the database changed, one `erDiagram` of the affected tables.

Then append one line to `docs/reports/INDEX.md`:
`- <yyyy-MM-dd HH:mm> — <task> — [folder](./<folder>/)`

End your reply with: `📄 Reports written to docs/reports/<folder>/`.

## 6. Project milestones (do in order, one prompt each)

1. Create the solution, `global.json` (SDK 10.0.x) and the 5 projects on `net10.0` + Android app shell (`/spec` then `/plan` first).
2. `Klasly.Core` entities + `Klasly.Infrastructure` Fluent API configuration, seed data, first migration on MSSQL; compare with `Klasly_Schema.sql`.
3. Web API auth: login, JWT with role claim, password hashing, role policies, OpenAPI + Swagger UI.
4. Web API admin CRUD: terms, subjects, classes, enrollments, accounts (soft delete).
5. Web MVC login + Admin pages.
6. Web API sessions: open session, QR token generation, scan endpoint with all 7 checks.
7. Web MVC Teacher session page: rotating QR + live attendance list. *(vertical slice, web side)*
8. Android login + Scan QR screen. *(vertical slice complete: teacher opens → student scans → teacher sees)*
9. Worker: set up Hangfire (SQL Server storage, `WorkerTickJob` recurring job) + close ended sessions and mark Absent; teacher attendance corrections in API + MVC.
10. Assessments and grades in API + MVC, with FCM grade-posted push.
11. Worker: risk calculation + RiskFlags snapshots (added as a step in `WorkerTickJob`).
12. Worker: Notifications outbox + Brevo sender with retry/backoff (added as a step in `WorkerTickJob`).
13. Dashboards: Parent portal (MVC), My attendance / My grades / Child overview (Android).
14. Hardening: role-access tests, boundary tests (exactly 20% / exactly 75), deploy to a host the phone can reach.
15. `/review` of the whole solution; fix findings.
