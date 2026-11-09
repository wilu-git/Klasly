# AI Development Guide — Klasly (ASP.NET Core)

## 1. Purpose

This guide defines the standards and instructions an AI assistant must follow when generating, modifying, or reviewing code for **Klasly — Smart Campus Attendance and Academic Monitoring**.

The C# side of the system has three projects:

| Project | Role |
| --- | --- |
| **ASP.NET Core MVC Web** | Admin, Teacher and Parent portal (Razor Views) |
| **ASP.NET Core Web API** | The single source of business logic and the only C# app besides the Worker that touches SQL Server |
| **ASP.NET Core Console App (Worker Service)** | Scheduled background jobs and outbound email |

The application uses:

- C#
- ASP.NET Core MVC and ASP.NET Core Web API
- Entity Framework Core (code-first migrations)
- Microsoft SQL Server
- Service–Repository Pattern

The Kotlin Android app is out of scope for this guide. From the C# side, only the Web API contract (endpoints, DTOs, JWT) matters to it.

The AI must follow the existing project architecture and coding conventions instead of introducing unrelated architectural patterns.

> Projects: `src/Klasly.Core` (entities, DTOs, interfaces), `src/Klasly.Infrastructure` (EF Core class library: `KlaslyDbContext`, configurations, migrations, EF Core repositories), `src/Klasly.API`, `src/Klasly.Web`, `src/Klasly.Worker`. The Web API and Worker reference both libraries; the MVC Web project must **not** reference either. See Section 27.4.

---

# 2. Core Architecture

Every C# project follows the same layered shape: **Controller (or job) → Service → Repository**. What differs is the repository's data source.

### Web API and Console Worker

```text
API Controller / Scheduled Job
        ↓
Service
        ↓
Repository
        ↓
Entity Framework Core / DbContext
        ↓
SQL Server
```

### MVC Web

```text
MVC Controller
        ↓
Service
        ↓
Repository (API-backed)
        ↓
Typed HttpClient
        ↓
Klasly Web API
```

The MVC project **never touches the database**. It has no `DbContext`, no EF Core package reference, and no connection string. Its repositories get and save data by calling the Web API.

### Why

The Android app can only reach data through the Web API, so every business rule (attendance status, score limits, ownership checks, soft deletes) must live in the Web API anyway. If MVC also wrote to SQL Server, each rule would have to exist twice, and any rule missing from MVC would be a hole.

### Responsibilities

#### Controller

Controllers are responsible for:

- Receiving HTTP requests.
- Validating request-level input.
- Calling the appropriate service.
- Returning Views (MVC), JSON (Web API), redirects, or appropriate HTTP responses.
- Handling MVC- or API-specific concerns.

Controllers should **not**:

- Directly access `DbContext` or `HttpClient`.
- Contain business logic.
- Contain database or HTTP operations.

Example (MVC):

```csharp
public async Task<IActionResult> Index(int classId)
{
    var sessions = await _classSessionService.GetByClassIdAsync(classId);

    return View(sessions);
}
```

---

## 3. Service Layer

Services contain business logic and application-level operations.

Services are responsible for:

- Applying business rules.
- Coordinating multiple repository calls.
- Transforming data when appropriate.
- Updating entity properties based on business requirements.
- Calling repositories to persist changes.

In Klasly, **business rules live in the Web API and Worker services**. MVC services coordinate calls and shape data for Views; they must not re-implement rules the Web API already enforces.

Example (Web API):

```csharp
public async Task<Grade> PostGradeAsync(int assessmentId, int studentId, decimal score)
{
    var assessment = await _assessmentRepository.GetByIdAsync(assessmentId)
        ?? throw new KeyNotFoundException("Assessment not found.");

    if (score < 0 || score > assessment.MaxScore)
        throw new ValidationException("Score must be between 0 and the maximum score.");

    return await _gradeRepository.UpsertAsync(assessmentId, studentId, score);
}
```

The service should not create raw SQL queries unless the existing project explicitly requires it.

---

# 4. Repository Layer

Repositories are responsible for data access. Klasly has two kinds.

### EF Core repositories (Web API, Worker)

Responsible for:

- Querying entities.
- Adding entities.
- Updating entities.
- Soft-deleting entities (see Section 27.7 — no hard deletes).
- Saving changes when that is the established project convention.
- Encapsulating Entity Framework Core database operations.

Example:

```csharp
public async Task<List<ClassSession>> GetByClassIdAsync(int classId)
{
    return await _context.ClassSessions
        .AsNoTracking()
        .Where(x => x.ClassId == classId)
        .ToListAsync();
}
```

Update example:

```csharp
public async Task<List<AttendanceRecord>> UpdateRangeAsync(
    List<AttendanceRecord> records)
{
    _context.AttendanceRecords.UpdateRange(records);

    await _context.SaveChangesAsync();

    return records;
}
```

### API-backed repositories (MVC)

Responsible for:

- Calling Web API endpoints through a typed `HttpClient`.
- Mapping API responses (DTOs) to what the MVC service needs.
- Translating non-success HTTP status codes into meaningful results or exceptions.

Example:

```csharp
public async Task<List<ClassSessionDto>> GetByClassIdAsync(int classId)
{
    var sessions = await _httpClient.GetFromJsonAsync<List<ClassSessionDto>>(
        $"api/classes/{classId}/sessions");

    return sessions ?? new List<ClassSessionDto>();
}
```

---

# 5. Entity Framework Core Rules

Applies to the Web API and Worker only.

Use Entity Framework Core for database operations.

Prefer:

```csharp
_context.ClassSessions
```

over manually constructed SQL when EF Core can perform the operation cleanly.

Use asynchronous database operations:

```csharp
ToListAsync()
FirstOrDefaultAsync()
SingleOrDefaultAsync()
AnyAsync()
CountAsync()
SaveChangesAsync()
```

Avoid synchronous database calls such as:

```csharp
.ToList()
.First()
.SaveChanges()
```

when an asynchronous equivalent is available.

The database schema is **code-first**: the EF Core model and migrations create the database. See Section 27.7 for the conventions the model must reproduce.

---

# 6. Query Guidelines

Build queries in the repository layer.

Example:

```csharp
public async Task<List<Enrollment>> GetActiveByClassIdAsync(int classId)
{
    return await _context.Enrollments
        .Where(x => x.ClassId == classId && x.Status == EnrollmentStatus.Active)
        .ToListAsync();
}
```

Use projection when only specific fields are required:

```csharp
var result = await _context.AttendanceRecords
    .Where(x => x.SessionId == sessionId)
    .Select(x => new
    {
        x.StudentId,
        x.Status,
        x.ScannedAt
    })
    .ToListAsync();
```

Do not load an entire entity when only a small subset of data is required unless the entity itself is needed.

---

# 7. Entity Updates

When an entity is retrieved from the repository and passed to another application layer, that layer may modify the entity properties.

Example flow:

```text
Repository
    ↓
List<AttendanceRecord>
    ↓
Service
    ↓
Modify properties
    ↓
Repository
    ↓
UpdateRange()
    ↓
SaveChangesAsync()
```

Example (teacher correction):

```csharp
var record = await _attendanceRepository.GetByIdAsync(attendanceId);

record.Status = AttendanceStatus.Present;
record.Source = AttendanceSource.Manual;
record.MarkedBy = teacherUserId;
record.MarkedAt = DateTime.UtcNow;

await _attendanceRepository.UpdateAsync(record);
```

If the entities are tracked by the same `DbContext`, avoid unnecessary calls to `Attach()` or `Update()` for every individual entity.

Use `UpdateRange()` when appropriate for detached entities or when the project's established repository implementation requires it.

---

# 8. LINQ Guidelines

Use LINQ for collection and database operations.

For database queries, make sure the expression can be translated by Entity Framework Core.

When working with a `List<T>`, LINQ methods such as `Select`, `Where`, `FirstOrDefault`, and `ToList` are valid.

Do not incorrectly assume a collection is `IEnumerable<dynamic>` if its actual type is a strongly typed list.

Prefer strongly typed models whenever possible.

---

# 9. Dependency Injection

Use ASP.NET Core dependency injection.

Example:

```csharp
public class ClassSessionService : IClassSessionService
{
    private readonly IClassSessionRepository _repository;

    public ClassSessionService(IClassSessionRepository repository)
    {
        _repository = repository;
    }
}
```

Registration in the Web API or Worker:

```csharp
builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
builder.Services.AddScoped<IClassSessionRepository, ClassSessionRepository>();
```

Registration in MVC (API-backed repository as a typed client):

```csharp
builder.Services.AddScoped<IClassSessionService, ClassSessionService>();
builder.Services.AddHttpClient<IClassSessionRepository, ClassSessionApiRepository>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
});
```

Do not instantiate services, repositories or `HttpClient` manually:

```csharp
var service = new ClassSessionService(...);
var client = new HttpClient();
```

---

# 10. Interfaces

Use interfaces for services and repositories.

Example:

```csharp
public interface IClassSessionRepository
{
    Task<List<ClassSession>> GetByClassIdAsync(int classId);

    Task<ClassSession?> GetByIdAsync(int sessionId);
}
```

Implementations:

```csharp
public class ClassSessionRepository : IClassSessionRepository      // Web API / Worker (EF Core)
{
}

public class ClassSessionApiRepository : IClassSessionRepository   // MVC (calls the Web API)
{
}
```

MVC interfaces work with DTOs, not EF entities, because the MVC project does not reference the entity model.

---

# 11. MVC Rules

Views should be responsible for presentation.

Do not place data access inside Razor views.

The MVC project must not:

- Reference `Microsoft.EntityFrameworkCore` or any EF Core provider package.
- Contain a `DbContext` or a connection string.
- Inject `HttpClient` into Views or controllers.

The flow is always:

```text
Controller → Service → API-backed Repository → Web API
```

The controller provides the required model to the view.

Authentication in MVC: the user logs in through the Web API (`POST /api/auth/login`). MVC stores the returned JWT in an **HttpOnly + Secure + SameSite cookie** and attaches it to Web API calls. Forms use anti-forgery tokens.

---

# 12. View Models

Use ViewModels when the view requires data that does not directly represent an API DTO.

Example:

```csharp
public class SessionAttendanceViewModel
{
    public int SessionId { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public List<AttendanceRowViewModel> Rows { get; set; } = new();
}
```

---

# 13. External API / HTTP Calls

All outbound HTTP goes through typed `HttpClient` clients registered with dependency injection.

| Caller | Client | Target |
| --- | --- | --- |
| MVC | API-backed repositories | Klasly Web API |
| Worker | `BrevoEmailClient` | Brevo Transactional Email API (`POST /v3/smtp/email`) |
| Web API | `FcmPushClient` | Firebase Cloud Messaging HTTP v1 |

Do not put external API calls directly inside controllers or jobs.

Example:

```csharp
public class BrevoEmailClient : IBrevoEmailClient
{
    private readonly HttpClient _httpClient;

    public BrevoEmailClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
}
```

The Brevo client uses a **10-second timeout**.

---

# 14. Configuration

Application configuration is stored in configuration files and accessed through configuration/options patterns.

Example (`appsettings.json`):

```json
{
  "Api": {
    "BaseUrl": "https://localhost:7001/"
  },
  "Brevo": {
    "BaseUrl": "https://api.brevo.com/v3/",
    "TimeoutSeconds": 10
  },
  "Worker": {
    "IntervalMinutes": 5
  }
}
```

Secrets live in **user-secrets** (development) or **environment variables** (deployed), never in `appsettings.json`, the database, documents or the repo:

- `Brevo:ApiKey`
- `Qr:SigningKey` (HMAC key for QR tokens)
- `Jwt:Key`
- Any connection string that contains a password (Web API and Worker only). A LocalDB / Windows-auth connection string with no password may live in `appsettings.Development.json`.
- FCM service-account credentials

Business thresholds (`AbsenceThreshold`, `PassingGrade`, `QrStepSeconds`, `MaxEmailAttempts`, `TimeZoneId`) live in the `SystemSettings` table, not in config files or code.

---

# 15. Error Handling

Use appropriate exception handling.

Do not silently swallow exceptions:

```csharp
try
{
    // operation
}
catch
{
}
```

If an exception is handled, log it or handle it meaningfully.

Example:

```csharp
try
{
    await _riskService.RecomputeAsync();
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error recomputing risk flags.");

    throw;
}
```

Do not expose sensitive exception details to end users. The Web API returns the status codes and messages defined in Section 27.8.

---

# 16. Logging

Use **Serilog** with structured logging.

Preferred:

```csharp
_logger.LogInformation(
    "Closing session {SessionId} for class {ClassId}",
    sessionId, classId);
```

Avoid:

```csharp
_logger.LogInformation(
    "Closing session " + sessionId);
```

Do not log:

- Passwords
- API keys
- Access tokens / JWTs
- QR signing keys
- Connection strings
- Sensitive personal information (student numbers, guardian emails, grades)

---

# 17. Naming Conventions

Use standard C# naming conventions.

### Classes

```csharp
ClassSessionService
ClassSessionRepository
ClassSessionApiRepository
SessionAttendanceViewModel
```

### Interfaces

```csharp
IClassSessionService
IClassSessionRepository
```

### Methods

```csharp
GetByClassIdAsync()
OpenSessionAsync()
MarkAbsentAsync()
```

### Variables

```csharp
sessionId
attendanceRecords
weightedAverage
```

### Private fields

```csharp
private readonly IClassSessionRepository _repository;
```

### Tables and entities

Tables use plain plural names with no prefix (`Users`, `ClassSessions`, `AttendanceRecords`). Entities use the singular (`User`, `ClassSession`, `AttendanceRecord`). Do not add a `Tbl` prefix.

---

# 18. Async/Await

Use asynchronous programming for I/O operations.

Preferred:

```csharp
public async Task<List<ClassSession>> GetByClassIdAsync(int classId)
{
    return await _repository.GetByClassIdAsync(classId);
}
```

Avoid blocking asynchronous calls:

```csharp
var result = service.GetByClassIdAsync(classId).Result;
```

or:

```csharp
var result = service.GetByClassIdAsync(classId).GetAwaiter().GetResult();
```

---

# 19. Database Performance

Avoid unnecessary database calls.

Bad (N+1):

```csharp
foreach (var enrollment in enrollments)
{
    var records = await repository.GetAttendanceByStudentIdAsync(enrollment.StudentId);
}
```

Prefer retrieving the required data in one appropriate query when practical. This matters most in the Worker's risk job, which runs for every enrollment every 5 minutes.

Also consider:

- Filtering in SQL instead of memory.
- Projection.
- `AsNoTracking()` for read-only queries.
- The indexes defined in Section 27.7.
- Avoiding unnecessary `Include()` calls.

---

# 20. Existing Code Takes Priority

When modifying an existing project:

1. Inspect the existing architecture.
2. Follow existing naming conventions.
3. Reuse existing interfaces.
4. Reuse existing repositories and services.
5. Avoid creating duplicate abstractions.
6. Do not move interfaces or classes unless explicitly requested.
7. Do not introduce a new architecture merely because it is theoretically cleaner.

The goal is to make the requested change while minimizing unrelated changes.

---

# 21. Do Not Over-Engineer

Do not introduce unnecessary:

- MediatR
- CQRS
- Generic repositories
- Unit of Work
- AutoMapper
- Domain-driven design
- Event sourcing
- Additional projects
- Additional abstraction layers

unless the project already uses them or the user explicitly requests them.

The required architecture is the one in Section 2:

```text
Web API / Worker:  Controller or Job → Service → Repository → EF Core → SQL Server
MVC:               Controller → Service → API-backed Repository → Web API
```

---

# 22. AI Response Requirements

When generating code, the AI should:

1. State which project (Web, Api, Worker) each file belongs to, and where in it.
2. Show complete relevant code.
3. Keep examples compatible with the project's ASP.NET Core version.
4. Use C#.
5. Use Entity Framework Core in the Web API and Worker only.
6. Follow the Service–Repository Pattern.
7. Prefer async database and HTTP operations.
8. Reuse existing project structures.
9. Avoid unrelated refactoring.
10. Point out assumptions when required.

When modifying existing code, show the changed section and clearly identify where it belongs.

---

# 23. How-To Guide Requirement

For implementation requests, the AI should provide a practical implementation guide.

The guide should generally contain:

```text
1. What will be changed (and in which projects)
2. Project/file structure
3. Entity + EF Core configuration (Web API)
4. Migration (Web API)
5. Repository interface + EF Core implementation (Web API / Worker)
6. Service interface + implementation (Web API / Worker)
7. API controller + DTOs (Web API)
8. API-backed repository (MVC)
9. MVC service, controller, View/ViewModel
10. Dependency Injection registration (each affected project)
11. Configuration if required
12. Database considerations
13. Testing steps
14. Common errors/troubleshooting
```

Only include sections relevant to the requested feature.

---

# 24. Standard Implementation Example

For a feature called `ClassSessions`:

```text
Klasly.Core/
    Entities/
        ClassSession.cs
    Interfaces/
        Repositories/
            IClassSessionRepository.cs

Klasly.Infrastructure/
    Data/
        KlaslyDbContext.cs
        Configurations/
            ClassSessionConfiguration.cs
        Migrations/
    Repositories/
        ClassSessionRepository.cs

Klasly.API/
    Controllers/
        ClassSessionsController.cs
    Services/
        Interfaces/
            IClassSessionService.cs
        ClassSessionService.cs
    Dtos/
        ClassSessionDto.cs

Klasly.Web/
    Controllers/
        ClassSessionsController.cs
    Services/
        Interfaces/
            IClassSessionService.cs
        ClassSessionService.cs
    Repositories/
        Interfaces/
            IClassSessionRepository.cs
        ClassSessionApiRepository.cs
    Dtos/
        ClassSessionDto.cs
    ViewModels/
        SessionAttendanceViewModel.cs
    Views/
        ClassSessions/
            Index.cshtml
```

Flow:

```text
Web ClassSessionsController
        ↓
Web IClassSessionService
        ↓
ClassSessionApiRepository  ──HTTP + JWT──►  Api ClassSessionsController
                                                    ↓
                                            Api IClassSessionService
                                                    ↓
                                            IClassSessionRepository
                                                    ↓
                                            EF Core → SQL Server
```

---

# 25. Final AI Checklist

Before providing a solution, verify:

- [ ] Is the code written in C#, in the correct project?
- [ ] Does the MVC project stay free of `DbContext`, EF Core and connection strings?
- [ ] Do MVC repositories call the Web API through a typed `HttpClient`?
- [ ] Is Entity Framework Core used for database access in the Web API / Worker?
- [ ] Is the Service–Repository Pattern followed?
- [ ] Do controllers avoid direct data access?
- [ ] Do business rules live in Web API / Worker services, not in MVC?
- [ ] Does the repository contain data-access logic only?
- [ ] Are async EF and HTTP operations used?
- [ ] Are interfaces and dependency injection used?
- [ ] Are secrets kept out of source code and config files?
- [ ] Are thresholds read from `SystemSettings`, not hard-coded?
- [ ] Are all timestamps stored in UTC?
- [ ] Does any delete use soft delete instead of removing rows?
- [ ] Does the EF Core model match the database conventions in Section 27.7?
- [ ] Does the solution avoid unnecessary architectural changes?
- [ ] Is the implementation guide included when requested?
- [ ] Are testing and troubleshooting steps included when relevant?

---

# 26. Priority Rules

When instructions conflict, use this priority:

1. Explicit user requirement.
2. Existing project architecture and conventions.
3. This AI development guide.
4. Standard ASP.NET Core / EF Core best practices.
5. General architectural preferences.

The AI should not override an explicit project requirement simply because another approach is considered more modern or theoretically cleaner.

---

# 27. Project Outline — Klasly

**Title:** Klasly — Smart Campus Attendance and Academic Monitoring
**Team:** Venalouie Cruz · Matt Linux Bondoc
**Sources:** Klasly Brainstorm Decisions v2, `Klasly ERD.dbml`, `Klasly_Schema.sql`, and the four project diagrams

## 27.1 Overview

Klasly replaces paper roll calls. Students check in by scanning a short-lived, signed QR code with the Android app. Teachers post grades. A background worker combines attendance and grades into early-warning **at-risk flags**, and emails parents through the Brevo API so they can act before it's too late.

**Roles:**
- **Admin:** manages terms, subjects, classes and accounts
- **Teacher:** runs sessions and posts grades
- **Student:** scans QR codes and views their records
- **Parent/Guardian:** monitors their linked child and receives alerts

## 27.2 Features

| Feature | Description | Category |
| --- | --- | --- |
| Role-based login | 4 roles. The Web API issues JWT, and every request gets server-side ownership checks | Core |
| Term, class & enrollment management | Admin manages academic terms, subjects, classes, teachers and enrollments | Core |
| Rotating signed QR session | Teacher opens a session on MVC. The Web API issues a new signed QR token every 30 s | Core |
| QR scan check-in | Student scans in the Android app. The Web API validates the scan and records Present or Late | Core |
| Auto-absent marking | After a session closes, the Worker marks enrolled non-scanners as Absent | Core |
| Attendance records & corrections | View records by class, session or student. Teachers can manually correct records (logged) | Core |
| Assessments & grade entry | Teacher creates weighted assessments and posts scores (0 to max score) | Core |
| Grade push notifications | Students get an FCM push when a grade is posted | Core |
| At-risk flagging | Absence rate ≥ 20% OR weighted average < 75. Thresholds are kept in `SystemSettings` | Core |
| Parent email alerts | The Worker queues absence and at-risk emails in the outbox, then sends them through Brevo | Core |
| Role dashboards | Summary views for each role | Core |
| Excuse / leave requests | Student submits an excuse. Once the teacher approves it, the record becomes Excused | Nice-to-Have |
| Export reports | Attendance and grade summaries as PDF/Excel | Nice-to-Have |
| Risk trend view | Chart of each student's risk snapshots over the term | Nice-to-Have |
| Offline scan queue | Save scans while offline and sync later (conflicts with QR expiry, so deferred) | Optional |
| GPS geofence check-in | Reject scans made outside campus | Optional |
| SMS alerts | SMS channel for parents (e.g., Semaphore) | Optional |
| LMS integration | Sync classes with Google Classroom | Optional |

Do not build Nice-to-Have or Optional features unless explicitly asked.

## 27.3 Technology stack

| Component | Purpose |
| --- | --- |
| **ASP.NET Core MVC Web** | Controllers + Razor Views for the Admin, Teacher and Parent portals. Displays the rotating QR. Reaches data only through API-backed repositories (typed HttpClient → Web API). JWT in HttpOnly cookie; anti-forgery on forms |
| **ASP.NET Core Web API** | REST endpoints (JSON). JWT issuing, role and ownership policies, QR token sign/verify (HMAC), attendance/grades/risk services, EF Core repositories, validation and error handling, Notifications outbox writer, FCM push sender, Swagger/OpenAPI, Serilog, password hashing (Identity hasher). **Single source of business logic** |
| **ASP.NET Core Console App (Worker Service)** | `BackgroundService` that runs every 5 minutes (from config): close ended sessions → mark Absent → recompute risk flags → queue alerts → send outbox emails through Brevo with retry, backoff and jitter |
| **Kotlin Android App** | Student and Parent app (Jetpack Compose + MVVM, CameraX + ML Kit, Retrofit/OkHttp, FCM, EncryptedSharedPreferences). Out of scope for this guide |
| **Microsoft SQL Server** | Persistent store. Schema created by EF Core code-first migrations |
| **Brevo Transactional Email API (v3)** | External API for parent alerts. `POST https://api.brevo.com/v3/smtp/email` with the `api-key` header. Free plan: 300 emails/day |
| **Firebase Cloud Messaging (HTTP v1)** | Grade-posted push notifications to Android, via service account |

**Communication:**
1. Teacher / Admin / Parent → MVC Web (HTTPS, HTML)
2. Student / Parent → Android app
3. MVC → Web API (REST + JWT)
4. Android → Web API (REST + JWT)
5. Web API → SQL Server (EF Core)
6. Web API → FCM (push request)
7. FCM → Android (push notification)
8. Console Worker → SQL Server (EF Core)
9. Console Worker → Brevo (HTTPS + api-key) → parent's inbox

## 27.4 Shared data project (decided)

- **Revised 2026-11-09:** the shared data layer is two class libraries, not one. Entities, DTOs and interfaces live in `src/Klasly.Core`; `KlaslyDbContext`, EF Core configurations, migrations and EF Core repositories live in `src/Klasly.Infrastructure`. The single `src/Klasly.Data` named in earlier drafts was never created — do not add it.
- `Klasly.API` and `Klasly.Worker` reference both libraries. Business-rule services stay in the Api and Worker projects.
- `Klasly.Web` references neither (Section 11) — it reaches data only through API-backed repositories.
- Migrations: `dotnet ef migrations add <Name> -p src/Klasly.Infrastructure -s src/Klasly.API`, then `dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API`.

## 27.5 Security & privacy rules

- Authorization is enforced in the Web API; hiding a button in MVC or Android is not security. A teacher can manage only their own classes, and a parent can see only linked students.
- Passwords are stored hashed (ASP.NET Core Identity hasher), never in plaintext.
- All `datetime2` columns store **UTC**. Convert to `TimeZoneId` (Asia/Manila, UTC+08:00) only for display.
- The QR signing key, Brevo api-key, JWT key and connection string live in user-secrets or environment variables (Section 14).
- Fictional test data only, in line with the Data Privacy Act of 2012 (RA 10173).
- The at-risk flag is an early-warning indicator for teachers to review, not a final judgment.

## 27.6 Database tables

| Table | Key fields | Purpose |
| --- | --- | --- |
| **Users** | UserId PK, FullName, Email (unique), PasswordHash, Role, FcmToken, RegisteredDeviceId, IsActive, CreatedAt, UpdatedAt | All login accounts |
| **Students** | StudentId PK, UserId FK (unique), StudentNumber (unique), YearLevel (1–6), Program | Student profile, 1–1 with Users |
| **Teachers** | TeacherId PK, UserId FK (unique), EmployeeNo (unique), Department | Teacher profile, 1–1 with Users |
| **Guardians** | GuardianId PK, UserId FK (unique), ContactEmail, ContactNumber | Parent/guardian profile, 1–1 with Users |
| **StudentGuardians** | (StudentId, GuardianId) composite PK, Relationship, ReceivesAlerts | Student ↔ Guardian (M:N) |
| **AcademicTerms** | TermId PK, Name (unique), StartDate, EndDate, IsActive | Groups classes by semester |
| **Subjects** | SubjectId PK, Code (unique), Title, Units (1–10) | Course catalog |
| **Classes** | ClassId PK, SubjectId FK, TeacherId FK, TermId FK, Section, Schedule, Room; unique (SubjectId, Section, TermId) | One subject offering per section per term |
| **Enrollments** | EnrollmentId PK, ClassId FK, StudentId FK, Status (Active/Dropped); unique (ClassId, StudentId) | Student ↔ Class (M:N) |
| **ClassSessions** | SessionId PK, ClassId FK, StartTime, EndTime, LateAfterMinutes (default 15, 0–120), QrNonce, Status (Scheduled/Open/Closed/Finalized/Cancelled), ClosedAt | One class meeting |
| **AttendanceRecords** | AttendanceId PK, SessionId FK, StudentId FK, Status (Present/Late/Absent/Excused), ScannedAt, MarkedAt, Source (Scan/Auto/Manual), MarkedBy FK→Users; unique (SessionId, StudentId) | One result per student per session |
| **ExcuseRequests** | ExcuseId PK, AttendanceId FK (unique), Reason, AttachmentPath, Status (Pending/Approved/Rejected), ReviewedBy FK→Users, ReviewedAt | Nice-to-have; 1–0..1 with AttendanceRecords |
| **Assessments** | AssessmentId PK, ClassId FK, Title, Type, MaxScore (> 0), WeightPercent (0–100], DatePosted; unique (ClassId, Title) | Graded activity of a class |
| **Grades** | GradeId PK, AssessmentId FK, StudentId FK, Score (≥ 0), PostedAt, UpdatedAt; unique (AssessmentId, StudentId) | One current score per student per assessment |
| **RiskFlags** | RiskFlagId PK, EnrollmentId FK, AbsenceRate (0–1), WeightedAverage (null or 0–100), IsAtRisk, ComputedAt | Snapshot per Worker run; dashboards read the latest |
| **Notifications** | NotificationId PK, UserId FK, Channel (Push/Email), Type (Absence/AtRisk/GradePosted), Subject, Body, Status (Pending/Accepted/Failed), Attempts (≥ 0), NextAttemptAt, LastError, DedupKey (unique), ExternalMessageId, CreatedAt | Outbox + log for push and email |
| **SystemSettings** | Key PK, Value, UpdatedBy FK→Users, UpdatedAt | AbsenceThreshold=0.20, PassingGrade=75, QrStepSeconds=30, MaxEmailAttempts=5, TimeZoneId=Asia/Manila |

## 27.7 Database conventions (EF Core model must reproduce these)

`Klasly_Schema.sql` is the reference specification. The EF Core model and migrations must produce an equivalent schema; compare with `dotnet ef migrations script` when in doubt.

- **Code-first:** the schema is created and changed only through EF Core migrations. Never edit the database by hand.
- **Enums as strings:** enum properties are stored as `nvarchar` with CHECK constraints listing the allowed values. Configure them with string conversion, never integers.
- **CHECK constraints:** reproduce every CHECK from the SQL file (role values, status values, YearLevel 1–6, Units 1–10, EndTime > StartTime, LateAfterMinutes 0–120, MaxScore > 0, WeightPercent > 0 and ≤ 100, Score ≥ 0, AbsenceRate 0–1, WeightedAverage 0–100, Attempts ≥ 0, ScannedAt required when Source = Scan, term EndDate > StartDate).
- **Unique rules:** every unique constraint listed in Section 27.6, plus the filtered unique index allowing **only one active term** (`IsActive = 1`).
- **No cascade deletes:** every foreign key uses `NoAction`. SQL Server rejects multiple cascade paths, and records must not disappear.
- **Soft delete only:** "deleting" means setting `IsActive = false` (Users, AcademicTerms) or `Status = Dropped` (Enrollments). Never remove rows.
- **UTC defaults:** `CreatedAt`, `MarkedAt`, `PostedAt` and `ComputedAt` default to `SYSUTCDATETIME()`. Columns are `datetime2(0)`.
- **Indexes:** `ClassSessions (Status, EndTime)`, `AttendanceRecords (StudentId, Status)`, `Grades (StudentId)`, `RiskFlags (EnrollmentId, ComputedAt DESC)`, `Notifications (Status, NextAttemptAt)`.
- **Seed data:** the five `SystemSettings` rows are seeded through the model.
- **Rules the database cannot enforce** and the Web API services must: Score ≤ the assessment's MaxScore; assessment weights per class total 100%; ownership checks.

## 27.8 Business rules

### QR attendance check-in

- The teacher opens a session from MVC → `POST /api/sessions/{id}/open` → the Web API sets `Status = Open`.
- The Web API generates the QR token; the MVC page fetches and displays a new one every 30 s.
- The token encodes `SessionId | timeStep | signature`. `timeStep = floor(UtcNow / QrStepSeconds)`. The signature is an HMAC of (SessionId, timeStep, QrNonce) using `Qr:SigningKey`.
- The Web API accepts the current and the previous time step (1-step grace for network delay).
- The 30 s refresh is not the session length; a 90-minute class shows a new QR every 30 s throughout.
- Scan endpoint: `POST /api/attendance/scan` (JWT + QR token). Validation order and responses:

| # | Check | On failure |
| --- | --- | --- |
| 1 | Valid JWT and role = Student | 401 "Session expired – please log in" |
| 2 | QR signature valid (HMAC) | 400 "Invalid QR code" |
| 3 | Time step is current or previous | 400 "QR expired – rescan" |
| 4 | Session status = Open | 409 "Session is closed" |
| 5 | Student enrolled in the class | 403 "Not enrolled in this class" |
| 6 | Scan from the registered device | 403 "Unregistered device" |
| 7 | Not already checked in | 409 "Already checked in" |

- ScannedAt ≤ StartTime + LateAfterMinutes → **Present**, otherwise **Late**. Insert the AttendanceRecord with `Source = Scan` and UTC times, and return `200 OK { status: Present | Late }`.
- A simultaneous duplicate scan is rejected by the unique (SessionId, StudentId) constraint; translate that database error to 409.
- The teacher's live attendance list updates by polling.
- Known limitation: a QR screenshot can be shared during its ~30–60 s window. Mitigated, not eliminated, by the short window, device binding and teacher headcount.

### Email outbox (Brevo) — Console Worker

1. **Trigger (Worker):** the Worker is the only component that queues email alerts. Each tick it picks up every new Absent record — whether marked automatically (`Source = Auto`) or by a teacher correction (`Source = Manual`) — and every student newly flagged at-risk, then builds the alert (guardian email, template, DedupKey such as `absent:{SessionId}:{StudentId}:{GuardianId}`) for each guardian with `ReceivesAlerts = 1`.
2. If the DedupKey already exists, skip — no duplicate alert. Otherwise insert a Notification with `Status = Pending`.
3. **Sender (Worker, every 5 min):** select Pending rows where `NextAttemptAt ≤ now`. If none, wait for the next tick.
4. Build the JSON payload with the api-key header and send `POST /v3/smtp/email` (HTTPS, 10 s timeout).
5. Results:
   - **2xx** → save `ExternalMessageId` (Brevo messageId), `Status = Accepted`.
   - **400/401/403** (permanent) → `Status = Failed`, save LastError, shown on the admin dashboard.
   - **429, 5xx or timeout** (transient) → `Attempts++`, `NextAttemptAt = now + backoff + jitter`, keep `Pending`.
   - **Attempts ≥ MaxEmailAttempts (5)** → `Status = Failed`, admin review.
6. Accepted means Brevo took the request, not that the parent read the email.

### Grade-posted push — Web API

When a grade is posted, the Web API sends an FCM push to the student (async) and logs it in Notifications with `Channel = Push`, `Type = GradePosted`.

### Risk calculation — Console Worker

- **Absence Rate** = Absent sessions ÷ Finalized sessions. Excused records and Cancelled sessions are excluded from both counts.
- **Weighted Average** = Σ(score/max × 100 × weight) ÷ Σ(weights of posted assessments). Ungraded assessments are excluded; the value is `NULL` until a grade is posted.
- **At-risk** if Absence Rate ≥ `AbsenceThreshold` **OR** Weighted Average < `PassingGrade`. Exactly 75 → not at-risk; exactly 20% → at-risk.
- Each run writes a RiskFlags snapshot per enrollment. When a student becomes at-risk (was not at-risk in the previous snapshot), queue an email to each guardian with `ReceivesAlerts = 1`.

## 27.9 Functional requirements

1. The system shall allow Admins, Teachers, Students and Parents to log in, and shall restrict data access by role and ownership on the server.
2. The system shall allow admins to manage academic terms, subjects, classes and enrollments.
3. The system shall let a teacher open a class session that displays a signed QR code which refreshes every 30 seconds.
4. The system shall record attendance as Present or Late when a student scans a valid QR within its validity window, and shall reject invalid, expired, duplicate or non-enrolled scans with a clear message.
5. The system shall automatically mark enrolled students who did not scan as Absent after a session closes.
6. The system shall allow teachers to create weighted assessments and post grades, validating that scores are between 0 and the maximum score.
7. The system shall send a push notification to a student when a grade is posted.
8. The system shall flag a student as at-risk when their absence rate reaches 20% or their weighted average falls below 75.
9. The system shall queue and send email alerts to linked guardians through the Brevo API, retrying transient failures and never sending the same alert twice.
10. The system shall display an appropriate error message when an operation or external request fails.

## 27.10 Development priority

1. **Vertical slice:** teacher opens session → student scans → API validates → DB saves → teacher sees attendance.
2. Absent marking + grades + risk.
3. Brevo outbox + FCM.
4. Hardening: role-access tests, boundary tests (exactly 20% / exactly 75), deployment to a real host the phone can reach.
