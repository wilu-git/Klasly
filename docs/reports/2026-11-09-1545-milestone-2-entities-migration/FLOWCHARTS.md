# Flowcharts — Milestone 2: model, configuration and first migration

## 1. What happens when a migration is created and applied

```mermaid
flowchart TD
    A[Developer runs<br/>dotnet ef migrations add InitialCreate] --> B[EF builds Klasly.API<br/>reads AddInfrastructure registration]
    B --> C{KlaslyDbContext found?}
    C -- no --> C1[Error: no DbContext<br/>in the startup project] --> Z([stop])
    C -- yes --> D[OnModelCreating]
    D --> E[ApplyConfigurationsFromAssembly<br/>17 IEntityTypeConfiguration classes]
    E --> F[SeedKlasly<br/>HasData: settings + fictional test data]
    F --> G{Model differs from<br/>KlaslyDbContextModelSnapshot?}
    G -- no --> G1[No changes; no migration written] --> Z
    G -- yes --> H[Write Migrations/..._InitialCreate.cs<br/>+ updated snapshot]
    H --> I[Developer runs<br/>dotnet ef database update]
    I --> J{Database reachable?}
    J -- no --> J1[Error: cannot connect<br/>start LocalDB] --> Z
    J -- yes --> K{Tables already exist<br/>from another model?}
    K -- yes --> K1[Error 2714:<br/>object already exists<br/>drop the old database first] --> Z
    K -- no --> L[CREATE TABLE x17<br/>CHECK x21, FK x22 NO ACTION,<br/>unique + filtered + perf indexes]
    L --> M[INSERT seed rows<br/>with IDENTITY_INSERT]
    M --> N[Row written to __EFMigrationsHistory]
    N --> O([Klasly database ready])
```

## 2. Layers involved in creating the schema

```mermaid
sequenceDiagram
    participant Dev as Developer (CLI)
    participant API as Klasly.API (startup project)
    participant Infra as Klasly.Infrastructure (EF Core)
    participant Core as Klasly.Core (entities)
    participant Db as MSSQL (LocalDB)

    Dev->>API: dotnet ef database update -p Infrastructure -s API
    API->>Infra: AddInfrastructure(connection string)
    Infra->>Infra: AddDbContext<KlaslyDbContext>(UseSqlServer)
    Infra->>Core: read entities + enums
    Infra->>Infra: apply 17 configurations, then SeedKlasly()
    Infra->>Db: SELECT MigrationId FROM __EFMigrationsHistory
    Db-->>Infra: (empty)
    Infra->>Db: CREATE TABLE / CHECK / FK / INDEX (17 tables)
    Infra->>Db: INSERT seed rows
    Infra->>Db: INSERT INTO __EFMigrationsHistory
    Db-->>Dev: Done
```

## 3. The 17 tables created by this migration

```mermaid
erDiagram
    Users ||--o| Students : "1-1"
    Users ||--o| Teachers : "1-1"
    Users ||--o| Guardians : "1-1"
    Users ||--o{ Notifications : "recipient"
    Users ||--o{ SystemSettings : "UpdatedBy"
    Students ||--o{ StudentGuardians : ""
    Guardians ||--o{ StudentGuardians : ""
    AcademicTerms ||--o{ Classes : ""
    Subjects ||--o{ Classes : ""
    Teachers ||--o{ Classes : "owns"
    Classes ||--o{ Enrollments : ""
    Classes ||--o{ ClassSessions : ""
    Classes ||--o{ Assessments : ""
    Students ||--o{ Enrollments : ""
    Enrollments ||--o{ RiskFlags : "snapshots"
    ClassSessions ||--o{ AttendanceRecords : ""
    Students ||--o{ AttendanceRecords : ""
    Users ||--o{ AttendanceRecords : "MarkedBy"
    AttendanceRecords ||--o| ExcuseRequests : "0..1"
    Users ||--o{ ExcuseRequests : "ReviewedBy"
    Assessments ||--o{ Grades : ""
    Students ||--o{ Grades : ""

    Users {
        int UserId PK
        nvarchar Email UK
        nvarchar Role "Admin|Teacher|Student|Parent"
        bit IsActive "soft delete"
        datetime2 CreatedAt "UTC"
    }
    AcademicTerms {
        int TermId PK
        nvarchar Name UK
        date StartDate
        date EndDate
        bit IsActive "only one = 1"
    }
    ClassSessions {
        int SessionId PK
        int ClassId FK
        datetime2 StartTime "UTC"
        datetime2 EndTime "UTC"
        int LateAfterMinutes "default 15"
        nvarchar QrNonce
        nvarchar Status "Scheduled|Open|Closed|Finalized|Cancelled"
    }
    AttendanceRecords {
        int AttendanceId PK
        int SessionId FK
        int StudentId FK
        nvarchar Status "Present|Late|Absent|Excused"
        nvarchar Source "Scan|Auto|Manual"
        datetime2 MarkedAt "UTC"
    }
    RiskFlags {
        int RiskFlagId PK
        int EnrollmentId FK
        decimal AbsenceRate "0..1"
        decimal WeightedAverage "null|0..100"
        bit IsAtRisk
        datetime2 ComputedAt "UTC"
    }
    Notifications {
        int NotificationId PK
        int UserId FK
        nvarchar Channel "Push|Email"
        nvarchar Type "Absence|AtRisk|GradePosted"
        nvarchar Status "Pending|Accepted|Failed"
        nvarchar DedupKey UK
    }
```
