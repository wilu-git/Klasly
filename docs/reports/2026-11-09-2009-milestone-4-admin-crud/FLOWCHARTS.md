# Milestone 4 — Admin CRUD · Flowcharts

## Request logic (every admin endpoint)

```mermaid
flowchart TD
    A[Admin sends request with JWT] --> B{Valid JWT?}
    B -- no --> E401[401 Unauthorized]
    B -- yes --> C{Role = Admin?}
    C -- no --> E403[403 Forbidden]
    C -- yes --> D{Body passes data annotations?<br/>required, lengths, ranges}
    D -- no --> E400a[400 validation problem-details]
    D -- yes --> F{Id in URL exists?}
    F -- no --> E404[404 not found]
    F -- yes --> G{Business rules OK?<br/>ids in body exist, teacher/student active,<br/>end date after start, role profile fields,<br/>not deactivating yourself}
    G -- no --> E400b[400 problem-details with message]
    G -- yes --> H{Unique values free?<br/>code, name, email, student no.,<br/>employee no., class, enrollment, link}
    H -- no --> E409[409 conflict naming the value]
    H -- yes --> I[Repository saves via EF Core]
    I --> J{Unique index fired anyway?<br/>race between two saves}
    J -- yes --> E409b[409 'That value is already used.']
    J -- no --> K[200 / 201 / 204 with DTO]
```

## Activate a term (layers)

```mermaid
sequenceDiagram
    actor Admin
    participant C as TermsController
    participant S as TermService
    participant R as TermRepository
    participant DB as SQL Server (Klasly)

    Admin->>C: POST /api/terms/2/activate (Bearer JWT)
    Note over C: [Authorize(Policy = "AdminOnly")]
    C->>S: ActivateAsync(2)
    S->>R: GetByIdAsync(2)
    R->>DB: SELECT ... WHERE TermId = 2
    DB-->>R: term (tracked)
    alt term not found
        S-->>C: throw KeyNotFoundException
        C-->>Admin: 404 problem-details (ApiExceptionHandler)
    else found
        S->>R: ActivateAsync(term)
        R->>DB: BEGIN TRANSACTION
        R->>DB: UPDATE AcademicTerms SET IsActive = 0 WHERE IsActive = 1 AND TermId <> 2
        R->>DB: UPDATE AcademicTerms SET IsActive = 1 WHERE TermId = 2
        R->>DB: COMMIT
        S-->>C: TermResponse
        C-->>Admin: 200 { termId: 2, isActive: true }
    end
```

## Enroll a student (layers)

```mermaid
sequenceDiagram
    actor Admin
    participant C as EnrollmentsController
    participant S as EnrollmentService
    participant CR as ClassRepository
    participant UR as UserRepository
    participant ER as EnrollmentRepository
    participant DB as SQL Server

    Admin->>C: POST /api/enrollments { classId, studentId }
    C->>S: EnrollAsync(request)
    S->>CR: GetByIdAsync(classId)
    S->>UR: GetStudentAsync(studentId) (with User)
    Note over S: missing class/student or inactive student → 400
    S->>ER: GetByClassAndStudentAsync(classId, studentId)
    ER->>DB: SELECT ... (unique pair)
    alt row is Active
        C-->>Admin: 409 already enrolled
    else row is Dropped
        S->>ER: Status = Active, SaveChangesAsync
        C-->>Admin: 200 (same enrollmentId)
    else no row
        S->>ER: AddAsync(new Enrollment)
        ER->>DB: INSERT Enrollments
        C-->>Admin: 201 Created
    end
```

## Tables this milestone reads and writes (no schema change)

```mermaid
erDiagram
    Users ||--o| Students : "1-1 profile"
    Users ||--o| Teachers : "1-1 profile"
    Users ||--o| Guardians : "1-1 profile"
    Students ||--o{ StudentGuardians : ""
    Guardians ||--o{ StudentGuardians : "link / unlink"
    AcademicTerms ||--o{ Classes : ""
    Subjects ||--o{ Classes : ""
    Teachers ||--o{ Classes : "owns"
    Classes ||--o{ Enrollments : ""
    Students ||--o{ Enrollments : ""

    Users { int UserId PK
        nvarchar Email UK
        nvarchar Role
        bit IsActive "soft delete" }
    AcademicTerms { int TermId PK
        nvarchar Name UK
        bit IsActive "only one = 1" }
    Subjects { int SubjectId PK
        nvarchar Code UK }
    Classes { int ClassId PK
        int SubjectId FK
        int TeacherId FK
        int TermId FK
        nvarchar Section "UK with SubjectId + TermId" }
    Enrollments { int EnrollmentId PK
        int ClassId FK
        int StudentId FK
        nvarchar Status "Active / Dropped" }
    StudentGuardians { int StudentId PK
        int GuardianId PK
        bit ReceivesAlerts }
```
