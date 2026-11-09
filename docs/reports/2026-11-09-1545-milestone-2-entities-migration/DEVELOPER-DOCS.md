# Developer docs — Milestone 2: Core entities, EF Core configuration, seed, first migration

## Summary

Asked: start Milestone 2 — `Klasly.Core` entities plus `Klasly.Infrastructure` Fluent API
configuration, seed data and the first migration on MSSQL, then compare with `Klasly_Schema.sql`.

Done: 17 entities and 9 string-stored enums in `Klasly.Core/Entities/`; `KlaslyDbContext` with one
`IEntityTypeConfiguration` per table in `Klasly.Infrastructure/Data/Configurations/`; model seed
(5 `SystemSettings` rows + fictional test data) in `Data/KlaslySeed.cs`; migration `InitialCreate`.
The migration was applied to a throwaway LocalDB database and produces 17 tables, 21 CHECK
constraints, 22 foreign keys and all seed rows — an equivalent schema to `Klasly_Schema.sql`.

**Not applied to the `Klasly` database itself** — see *Known limits* below; that database still
holds an unrelated 37-table model from an earlier attempt and dropping it needs the team's go-ahead.

## Projects touched

| Project | Change |
| --- | --- |
| **DOMAIN (`Klasly.Core`)** | 17 entities + enums; `Class1.cs` removed |
| **EF CORE (`Klasly.Infrastructure`)** | `KlaslyDbContext`, 17 configurations, seed, migration, EF packages, `AddInfrastructure` |
| **WEB API (`Klasly.API`)** | registers the context (also the design-time target of `dotnet ef`); dev connection string |
| **MSSQL** | migration `20261109074245_InitialCreate` (verified on a scratch database, not yet on `Klasly`) |
| WEB MVC / CONSOLE WORKER / ANDROID KOTLIN | untouched |

## Files changed

| Path | Change | Why |
| --- | --- | --- |
| `global.json` | added | Pins the SDK to 10.0.403 (`rollForward: latestFeature`) as CLAUDE.md §3 requires; it was missing. |
| `src/Klasly.Core/Entities/Enums.cs` | added | The 9 enums stored as `nvarchar` + CHECK (guide §27.7). |
| `src/Klasly.Core/Entities/User.cs` … `SystemSetting.cs` (17 files) | added | One entity per table in guide §27.6. |
| `src/Klasly.Core/Class1.cs` | deleted | Template placeholder. |
| `src/Klasly.Infrastructure/Data/KlaslyDbContext.cs` | added | The single context: 17 `DbSet`s, applies the configurations and the seed. |
| `src/Klasly.Infrastructure/Data/KlaslySeed.cs` | added | `HasData` seed: settings + fictional accounts, term, subjects, classes, enrollments. |
| `src/Klasly.Infrastructure/Data/Configurations/*.cs` (17 files) | added | Fluent API per table: lengths, enum-to-string, CHECKs, unique rules, indexes, `NoAction` FKs. |
| `src/Klasly.Infrastructure/Migrations/20261109074245_InitialCreate*.cs` + snapshot | added | The first migration. |
| `src/Klasly.Infrastructure/DependencyInjection.cs` | modified | `AddInfrastructure(connectionString)` registers `KlaslyDbContext` on SQL Server. |
| `src/Klasly.Infrastructure/Klasly.Infrastructure.csproj` | modified | EF Core SqlServer + Design 10.0.14; `System.Security.Cryptography.Pkcs` 10.0.14 pin. |
| `src/Klasly.API/Program.cs` | modified | Calls `AddInfrastructure(...)`; `dotnet ef` needs a context in the startup project. |
| `src/Klasly.API/appsettings.Development.json` | modified | LocalDB connection string `Klasly` (Windows auth, no password). |

## Key code

**Enum stored as a string, with the CHECK the SQL file has** (`UserConfiguration.cs`):

```csharp
builder.ToTable("Users", t => t.HasCheckConstraint(
    "CK_Users_Role", "[Role] IN (N'Admin', N'Teacher', N'Student', N'Parent')"));

builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
builder.Property(u => u.CreatedAt).HasColumnType("datetime2(0)").HasDefaultValueSql("SYSUTCDATETIME()");
builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UQ_Users_Email");
```

**One active term only** (`AcademicTermConfiguration.cs`):

```csharp
builder.HasIndex(t => t.IsActive)
    .IsUnique()
    .HasFilter("[IsActive] = 1")
    .HasDatabaseName("UX_AcademicTerms_OneActive");
```

**No cascade deletes** — every relationship ends in `.OnDelete(DeleteBehavior.NoAction)` with the
constraint name from the SQL file, e.g. `FK_AttendanceRecords_ClassSessions`.

**Seed** (`KlaslySeed.cs`) uses constants only, because `HasData` is baked into the migration:
a fixed `SeededAt` stamp and one shared ASP.NET Core Identity PBKDF2 hash for all test accounts.
The test password is written in `HOW-TO-USE.md`; the hash itself is in the seed file.

## Database changes

Migration **`20261109074245_InitialCreate`** creates all 17 tables of guide §27.6:
Users, Students, Teachers, Guardians, StudentGuardians, AcademicTerms, Subjects, Classes,
Enrollments, ClassSessions, AttendanceRecords, ExcuseRequests, Assessments, Grades, RiskFlags,
Notifications, SystemSettings — plus `__EFMigrationsHistory`.

Seed rows: 5 settings, 12 users (1 admin, 2 teachers, 6 students, 3 parents), 2 teachers,
6 students, 3 guardians, 6 student-guardian links, 1 term, 2 subjects, 2 classes, 9 enrollments.

### Comparison with `docs/database/Klasly_Schema.sql`

Equivalent. Identical: table and column names, types and lengths, `datetime2(0)` everywhere,
`date` for term dates, decimal precisions (6,2 / 5,2 / 5,4), all 21 CHECK constraints (same names
and same expressions), all 22 foreign keys (same names, all NO ACTION), every unique rule, the
filtered `UX_AcademicTerms_OneActive` index, the five listed indexes, and the `SYSUTCDATETIME()`
defaults.

Three harmless differences, all EF Core conventions:

| # | SQL file | Migration | Effect |
| --- | --- | --- | --- |
| 1 | `CONSTRAINT UQ_… UNIQUE (…)` | `CREATE UNIQUE INDEX [UQ_…]` with the same name and columns | None — in SQL Server a unique constraint *is* a unique index. |
| 2 | Named default constraints (`DF_Users_IsActive`, …) | Unnamed inline defaults with the same expressions | None at runtime; SQL Server auto-names them. |
| 3 | — | Extra non-unique indexes on single-column FKs: `IX_AttendanceRecords_MarkedBy`, `IX_Classes_TeacherId`, `IX_Classes_TermId`, `IX_ClassSessions_ClassId`, `IX_Enrollments_StudentId`, `IX_ExcuseRequests_ReviewedBy`, `IX_Notifications_UserId`, `IX_StudentGuardians_GuardianId`, `IX_SystemSettings_UpdatedBy` | EF indexes every FK by default; they help joins. Left in place. |

## How it was verified

```
dotnet build                      → Build succeeded, 0 errors, 0 warnings
dotnet ef migrations add InitialCreate -p src/Klasly.Infrastructure -s src/Klasly.API   → Done
dotnet ef migrations script --idempotent                                               → reviewed against Klasly_Schema.sql
dotnet ef database update --connection "...Database=Klasly_MigrationCheck..."           → Done
```

Counted on the scratch database, then dropped it: 17 tables, 21 check constraints, 22 foreign keys,
and the seed rows (settings 5, users 12, students 6, teachers 2, guardians 3, student-guardians 6,
terms 1, subjects 2, classes 2, enrollments 9).

No tests exist yet, so `dotnet test` was not run.

## Known limits / TODO

- **The `Klasly` LocalDB database was not migrated.** It already contains a 37-table model from an
  earlier attempt (`Programs`, `Roles`, `UserRoles`, `RefreshTokens`, `LoginAudits`, `TermGrades`, …)
  and a migration row `20261031065801_InitialCreate` that no longer exists in this repo. Applying
  `InitialCreate` fails with *"There is already an object named 'AcademicTerms'"*. It has to be
  dropped and recreated — a destructive step, so it is waiting on the team's go-ahead
  (`HOW-TO-USE.md` step 2 has the command).
- `Klasly.Worker` has no connection string and does not call `AddInfrastructure` yet — Milestone 9.
- No repositories yet (`Klasly.Infrastructure/Repositories/` is still empty) — Milestone 3 onwards.
- Rules the database cannot enforce stay for the Web API: `Score <= MaxScore`, assessment weights
  totalling 100% per class, and ownership checks.
- `Assessments.Type` is a free `nvarchar(30)`, exactly as in the SQL file (no CHECK, no enum).
- The seeded accounts all share one test password with a fixed salt. Fine for fictional test data,
  never for anything real.
- `dotnet-ef` is 10.0.11 while the runtime is 10.0.14; it works but prints a notice.
  `dotnet tool update -g dotnet-ef` silences it.
