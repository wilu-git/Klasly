/* =====================================================================
   Klasly — Smart Campus Attendance and Academic Monitoring
   Microsoft SQL Server schema (T-SQL) — matches "Klasly ERD.dbml" (17 tables)
   Team: Venalouie Cruz · Matt Linux Bondoc

   Conventions
   - Every datetime2 column stores UTC. Convert to Asia/Manila (UTC+08:00) only for display.
   - Enums from the ERD are nvarchar columns guarded by CHECK constraints.
   - Foreign keys use NO ACTION (the default). SQL Server rejects schemas with
     "multiple cascade paths" (e.g. Classes -> Enrollments and Students -> Enrollments),
     so deletes are handled in the API (soft-delete via IsActive / Status instead).
   - Composite UNIQUE constraints enforce the business rules
     (one attendance per student per session, one grade per assessment, ...).
   ===================================================================== */

-- Optional: create and switch to the database
IF DB_ID(N'Klasly') IS NULL
    CREATE DATABASE Klasly;
GO
USE Klasly;
GO

/* ---------------------------------------------------------------------
   1. ACCOUNTS & PEOPLE
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Users (
    UserId              int             IDENTITY(1,1) NOT NULL,
    FullName            nvarchar(150)   NOT NULL,
    Email               nvarchar(256)   NOT NULL,
    PasswordHash        nvarchar(500)   NOT NULL,   -- ASP.NET Core Identity hasher, never plaintext
    Role                nvarchar(20)    NOT NULL,
    FcmToken            nvarchar(500)   NULL,
    RegisteredDeviceId  nvarchar(200)   NULL,       -- one registered phone per student account
    IsActive            bit             NOT NULL CONSTRAINT DF_Users_IsActive  DEFAULT (1),
    CreatedAt           datetime2(0)    NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt           datetime2(0)    NULL,
    CONSTRAINT PK_Users        PRIMARY KEY (UserId),
    CONSTRAINT UQ_Users_Email  UNIQUE (Email),
    CONSTRAINT CK_Users_Role   CHECK (Role IN (N'Admin', N'Teacher', N'Student', N'Parent'))
);
GO

CREATE TABLE dbo.Students (
    StudentId       int            IDENTITY(1,1) NOT NULL,
    UserId          int            NOT NULL,
    StudentNumber   nvarchar(20)   NOT NULL,
    YearLevel       int            NOT NULL,
    Program         nvarchar(50)   NOT NULL,        -- e.g. BSCpE
    CONSTRAINT PK_Students               PRIMARY KEY (StudentId),
    CONSTRAINT UQ_Students_UserId        UNIQUE (UserId),          -- 1-to-1 with Users
    CONSTRAINT UQ_Students_StudentNumber UNIQUE (StudentNumber),
    CONSTRAINT CK_Students_YearLevel     CHECK (YearLevel BETWEEN 1 AND 6),
    CONSTRAINT FK_Students_Users         FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE TABLE dbo.Teachers (
    TeacherId   int            IDENTITY(1,1) NOT NULL,
    UserId      int            NOT NULL,
    EmployeeNo  nvarchar(20)   NOT NULL,
    Department  nvarchar(100)  NULL,
    CONSTRAINT PK_Teachers            PRIMARY KEY (TeacherId),
    CONSTRAINT UQ_Teachers_UserId     UNIQUE (UserId),
    CONSTRAINT UQ_Teachers_EmployeeNo UNIQUE (EmployeeNo),
    CONSTRAINT FK_Teachers_Users      FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

CREATE TABLE dbo.Guardians (
    GuardianId     int            IDENTITY(1,1) NOT NULL,
    UserId         int            NOT NULL,
    ContactEmail   nvarchar(256)  NOT NULL,         -- Brevo alert recipient
    ContactNumber  nvarchar(20)   NULL,
    CONSTRAINT PK_Guardians         PRIMARY KEY (GuardianId),
    CONSTRAINT UQ_Guardians_UserId  UNIQUE (UserId),
    CONSTRAINT FK_Guardians_Users   FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

-- M:N  Students <-> Guardians
CREATE TABLE dbo.StudentGuardians (
    StudentId       int           NOT NULL,
    GuardianId      int           NOT NULL,
    Relationship    nvarchar(30)  NULL,             -- Mother, Father, Guardian...
    ReceivesAlerts  bit           NOT NULL CONSTRAINT DF_StudentGuardians_ReceivesAlerts DEFAULT (1),
    CONSTRAINT PK_StudentGuardians           PRIMARY KEY (StudentId, GuardianId),
    CONSTRAINT FK_StudentGuardians_Students  FOREIGN KEY (StudentId)  REFERENCES dbo.Students (StudentId),
    CONSTRAINT FK_StudentGuardians_Guardians FOREIGN KEY (GuardianId) REFERENCES dbo.Guardians (GuardianId)
);
GO

/* ---------------------------------------------------------------------
   2. ACADEMIC STRUCTURE
   --------------------------------------------------------------------- */

CREATE TABLE dbo.AcademicTerms (
    TermId     int           IDENTITY(1,1) NOT NULL,
    Name       nvarchar(50)  NOT NULL,              -- e.g. 1st Sem 2026-27
    StartDate  date          NOT NULL,
    EndDate    date          NOT NULL,
    IsActive   bit           NOT NULL CONSTRAINT DF_AcademicTerms_IsActive DEFAULT (0),
    CONSTRAINT PK_AcademicTerms       PRIMARY KEY (TermId),
    CONSTRAINT UQ_AcademicTerms_Name  UNIQUE (Name),
    CONSTRAINT CK_AcademicTerms_Dates CHECK (EndDate > StartDate)
);
GO

-- Only one term may be active at a time
CREATE UNIQUE INDEX UX_AcademicTerms_OneActive
    ON dbo.AcademicTerms (IsActive) WHERE IsActive = 1;
GO

CREATE TABLE dbo.Subjects (
    SubjectId  int            IDENTITY(1,1) NOT NULL,
    Code       nvarchar(20)   NOT NULL,             -- e.g. ELECT3
    Title      nvarchar(150)  NOT NULL,
    Units      int            NOT NULL,
    CONSTRAINT PK_Subjects       PRIMARY KEY (SubjectId),
    CONSTRAINT UQ_Subjects_Code  UNIQUE (Code),
    CONSTRAINT CK_Subjects_Units CHECK (Units BETWEEN 1 AND 10)
);
GO

-- One subject offering for one section in one term (a teacher can own many)
CREATE TABLE dbo.Classes (
    ClassId    int            IDENTITY(1,1) NOT NULL,
    SubjectId  int            NOT NULL,
    TeacherId  int            NOT NULL,
    TermId     int            NOT NULL,
    Section    nvarchar(30)   NOT NULL,             -- e.g. BSCpE 3-A
    Schedule   nvarchar(100)  NULL,                 -- e.g. MWF 07:30-09:00 (PH time)
    Room       nvarchar(30)   NULL,
    CONSTRAINT PK_Classes                    PRIMARY KEY (ClassId),
    CONSTRAINT UQ_Classes_Subject_Section_Term UNIQUE (SubjectId, Section, TermId),
    CONSTRAINT FK_Classes_Subjects      FOREIGN KEY (SubjectId) REFERENCES dbo.Subjects (SubjectId),
    CONSTRAINT FK_Classes_Teachers      FOREIGN KEY (TeacherId) REFERENCES dbo.Teachers (TeacherId),
    CONSTRAINT FK_Classes_AcademicTerms FOREIGN KEY (TermId)    REFERENCES dbo.AcademicTerms (TermId)
);
GO

-- M:N  Students <-> Classes
CREATE TABLE dbo.Enrollments (
    EnrollmentId  int           IDENTITY(1,1) NOT NULL,
    ClassId       int           NOT NULL,
    StudentId     int           NOT NULL,
    Status        nvarchar(20)  NOT NULL CONSTRAINT DF_Enrollments_Status DEFAULT (N'Active'),
    CONSTRAINT PK_Enrollments               PRIMARY KEY (EnrollmentId),
    CONSTRAINT UQ_Enrollments_Class_Student UNIQUE (ClassId, StudentId),
    CONSTRAINT CK_Enrollments_Status        CHECK (Status IN (N'Active', N'Dropped')),
    CONSTRAINT FK_Enrollments_Classes       FOREIGN KEY (ClassId)   REFERENCES dbo.Classes (ClassId),
    CONSTRAINT FK_Enrollments_Students      FOREIGN KEY (StudentId) REFERENCES dbo.Students (StudentId)
);
GO

/* ---------------------------------------------------------------------
   3. ATTENDANCE
   --------------------------------------------------------------------- */

CREATE TABLE dbo.ClassSessions (
    SessionId         int            IDENTITY(1,1) NOT NULL,
    ClassId           int            NOT NULL,
    StartTime         datetime2(0)   NOT NULL,      -- UTC
    EndTime           datetime2(0)   NOT NULL,      -- UTC
    LateAfterMinutes  int            NOT NULL CONSTRAINT DF_ClassSessions_LateAfter DEFAULT (15),
    QrNonce           nvarchar(64)   NOT NULL,      -- per-session random value; HMAC key lives in app config
    Status            nvarchar(20)   NOT NULL CONSTRAINT DF_ClassSessions_Status DEFAULT (N'Scheduled'),
    ClosedAt          datetime2(0)   NULL,          -- UTC
    CONSTRAINT PK_ClassSessions          PRIMARY KEY (SessionId),
    CONSTRAINT CK_ClassSessions_Times    CHECK (EndTime > StartTime),
    CONSTRAINT CK_ClassSessions_Late     CHECK (LateAfterMinutes BETWEEN 0 AND 120),
    CONSTRAINT CK_ClassSessions_Status   CHECK (Status IN (N'Scheduled', N'Open', N'Closed', N'Finalized', N'Cancelled')),
    CONSTRAINT FK_ClassSessions_Classes  FOREIGN KEY (ClassId) REFERENCES dbo.Classes (ClassId)
);
GO

-- Worker query: find Open sessions whose EndTime has passed
CREATE INDEX IX_ClassSessions_Status_EndTime ON dbo.ClassSessions (Status, EndTime);
GO

CREATE TABLE dbo.AttendanceRecords (
    AttendanceId  int            IDENTITY(1,1) NOT NULL,
    SessionId     int            NOT NULL,
    StudentId     int            NOT NULL,
    Status        nvarchar(20)   NOT NULL,
    ScannedAt     datetime2(0)   NULL,              -- UTC; NULL when Auto/Manual
    MarkedAt      datetime2(0)   NOT NULL CONSTRAINT DF_AttendanceRecords_MarkedAt DEFAULT (SYSUTCDATETIME()),
    Source        nvarchar(10)   NOT NULL,
    MarkedBy      int            NULL,              -- teacher who corrected it (Users)
    CONSTRAINT PK_AttendanceRecords                 PRIMARY KEY (AttendanceId),
    CONSTRAINT UQ_AttendanceRecords_Session_Student UNIQUE (SessionId, StudentId),   -- blocks duplicate scans
    CONSTRAINT CK_AttendanceRecords_Status CHECK (Status IN (N'Present', N'Late', N'Absent', N'Excused')),
    CONSTRAINT CK_AttendanceRecords_Source CHECK (Source IN (N'Scan', N'Auto', N'Manual')),
    CONSTRAINT CK_AttendanceRecords_ScanTime CHECK (Source <> N'Scan' OR ScannedAt IS NOT NULL),
    CONSTRAINT FK_AttendanceRecords_ClassSessions FOREIGN KEY (SessionId) REFERENCES dbo.ClassSessions (SessionId),
    CONSTRAINT FK_AttendanceRecords_Students      FOREIGN KEY (StudentId) REFERENCES dbo.Students (StudentId),
    CONSTRAINT FK_AttendanceRecords_MarkedBy      FOREIGN KEY (MarkedBy)  REFERENCES dbo.Users (UserId)
);
GO

-- Student dashboard / risk job: attendance by student
CREATE INDEX IX_AttendanceRecords_Student ON dbo.AttendanceRecords (StudentId, Status);
GO

-- Nice-to-have feature
CREATE TABLE dbo.ExcuseRequests (
    ExcuseId        int             IDENTITY(1,1) NOT NULL,
    AttendanceId    int             NOT NULL,
    Reason          nvarchar(500)   NOT NULL,
    AttachmentPath  nvarchar(300)   NULL,
    Status          nvarchar(20)    NOT NULL CONSTRAINT DF_ExcuseRequests_Status DEFAULT (N'Pending'),
    ReviewedBy      int             NULL,
    ReviewedAt      datetime2(0)    NULL,           -- UTC
    CONSTRAINT PK_ExcuseRequests              PRIMARY KEY (ExcuseId),
    CONSTRAINT UQ_ExcuseRequests_Attendance   UNIQUE (AttendanceId),   -- 1-to-0..1 with AttendanceRecords
    CONSTRAINT CK_ExcuseRequests_Status       CHECK (Status IN (N'Pending', N'Approved', N'Rejected')),
    CONSTRAINT FK_ExcuseRequests_Attendance   FOREIGN KEY (AttendanceId) REFERENCES dbo.AttendanceRecords (AttendanceId),
    CONSTRAINT FK_ExcuseRequests_ReviewedBy   FOREIGN KEY (ReviewedBy)   REFERENCES dbo.Users (UserId)
);
GO

/* ---------------------------------------------------------------------
   4. GRADES & RISK
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Assessments (
    AssessmentId   int            IDENTITY(1,1) NOT NULL,
    ClassId        int            NOT NULL,
    Title          nvarchar(100)  NOT NULL,         -- e.g. Quiz 1, Midterm Exam
    Type           nvarchar(30)   NOT NULL,
    MaxScore       decimal(6,2)   NOT NULL,
    WeightPercent  decimal(5,2)   NOT NULL,         -- per class should total 100 (checked in the API)
    DatePosted     datetime2(0)   NULL,             -- UTC
    CONSTRAINT PK_Assessments          PRIMARY KEY (AssessmentId),
    CONSTRAINT UQ_Assessments_Class_Title UNIQUE (ClassId, Title),
    CONSTRAINT CK_Assessments_MaxScore CHECK (MaxScore > 0),
    CONSTRAINT CK_Assessments_Weight   CHECK (WeightPercent > 0 AND WeightPercent <= 100),
    CONSTRAINT FK_Assessments_Classes  FOREIGN KEY (ClassId) REFERENCES dbo.Classes (ClassId)
);
GO

CREATE TABLE dbo.Grades (
    GradeId       int            IDENTITY(1,1) NOT NULL,
    AssessmentId  int            NOT NULL,
    StudentId     int            NOT NULL,
    Score         decimal(6,2)   NOT NULL,          -- 0 <= Score <= Assessments.MaxScore (upper bound checked in API)
    PostedAt      datetime2(0)   NOT NULL CONSTRAINT DF_Grades_PostedAt DEFAULT (SYSUTCDATETIME()),
    UpdatedAt     datetime2(0)   NULL,
    CONSTRAINT PK_Grades                    PRIMARY KEY (GradeId),
    CONSTRAINT UQ_Grades_Assessment_Student UNIQUE (AssessmentId, StudentId),
    CONSTRAINT CK_Grades_Score              CHECK (Score >= 0),
    CONSTRAINT FK_Grades_Assessments        FOREIGN KEY (AssessmentId) REFERENCES dbo.Assessments (AssessmentId),
    CONSTRAINT FK_Grades_Students           FOREIGN KEY (StudentId)    REFERENCES dbo.Students (StudentId)
);
GO

CREATE INDEX IX_Grades_Student ON dbo.Grades (StudentId);
GO

-- Snapshot written by the Console worker; dashboards read the latest per enrollment
CREATE TABLE dbo.RiskFlags (
    RiskFlagId       int            IDENTITY(1,1) NOT NULL,
    EnrollmentId     int            NOT NULL,
    AbsenceRate      decimal(5,4)   NOT NULL,       -- Absent / finalized sessions (Excused excluded)
    WeightedAverage  decimal(5,2)   NULL,           -- NULL until a grade is posted
    IsAtRisk         bit            NOT NULL,       -- AbsenceRate >= 0.20 OR WeightedAverage < 75
    ComputedAt       datetime2(0)   NOT NULL CONSTRAINT DF_RiskFlags_ComputedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_RiskFlags             PRIMARY KEY (RiskFlagId),
    CONSTRAINT CK_RiskFlags_AbsenceRate CHECK (AbsenceRate BETWEEN 0 AND 1),
    CONSTRAINT CK_RiskFlags_Average     CHECK (WeightedAverage IS NULL OR WeightedAverage BETWEEN 0 AND 100),
    CONSTRAINT FK_RiskFlags_Enrollments FOREIGN KEY (EnrollmentId) REFERENCES dbo.Enrollments (EnrollmentId)
);
GO

CREATE INDEX IX_RiskFlags_Enrollment_Latest ON dbo.RiskFlags (EnrollmentId, ComputedAt DESC);
GO

/* ---------------------------------------------------------------------
   5. NOTIFICATIONS (OUTBOX) & SETTINGS
   --------------------------------------------------------------------- */

CREATE TABLE dbo.Notifications (
    NotificationId     int             IDENTITY(1,1) NOT NULL,
    UserId             int             NOT NULL,     -- recipient
    Channel            nvarchar(10)    NOT NULL,
    Type               nvarchar(30)    NOT NULL,     -- Absence / AtRisk / GradePosted
    Subject            nvarchar(200)   NULL,
    Body               nvarchar(max)   NULL,
    Status             nvarchar(20)    NOT NULL CONSTRAINT DF_Notifications_Status   DEFAULT (N'Pending'),
    Attempts           int             NOT NULL CONSTRAINT DF_Notifications_Attempts DEFAULT (0),
    NextAttemptAt      datetime2(0)    NULL,         -- UTC; backoff + jitter
    LastError          nvarchar(500)   NULL,
    DedupKey           nvarchar(150)   NOT NULL,     -- e.g. absent:{SessionId}:{StudentId}:{GuardianId}
    ExternalMessageId  nvarchar(200)   NULL,         -- Brevo messageId
    CreatedAt          datetime2(0)    NOT NULL CONSTRAINT DF_Notifications_CreatedAt DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_Notifications          PRIMARY KEY (NotificationId),
    CONSTRAINT UQ_Notifications_DedupKey UNIQUE (DedupKey),          -- never send the same alert twice
    CONSTRAINT CK_Notifications_Channel  CHECK (Channel IN (N'Push', N'Email')),
    CONSTRAINT CK_Notifications_Status   CHECK (Status IN (N'Pending', N'Accepted', N'Failed')),
    CONSTRAINT CK_Notifications_Type     CHECK (Type IN (N'Absence', N'AtRisk', N'GradePosted')),
    CONSTRAINT CK_Notifications_Attempts CHECK (Attempts >= 0),
    CONSTRAINT FK_Notifications_Users    FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
);
GO

-- Worker query: SELECT ... WHERE Status = 'Pending' AND NextAttemptAt <= SYSUTCDATETIME()
CREATE INDEX IX_Notifications_Outbox ON dbo.Notifications (Status, NextAttemptAt);
GO

CREATE TABLE dbo.SystemSettings (
    [Key]      nvarchar(50)   NOT NULL,             -- bracketed: KEY is a reserved word
    [Value]    nvarchar(200)  NOT NULL,
    UpdatedBy  int            NULL,
    UpdatedAt  datetime2(0)   NULL,                 -- UTC
    CONSTRAINT PK_SystemSettings           PRIMARY KEY ([Key]),
    CONSTRAINT FK_SystemSettings_UpdatedBy FOREIGN KEY (UpdatedBy) REFERENCES dbo.Users (UserId)
);
GO

/* ---------------------------------------------------------------------
   6. DEFAULT SETTINGS (no secrets here — API keys stay in user-secrets / env vars)
   --------------------------------------------------------------------- */

INSERT INTO dbo.SystemSettings ([Key], [Value], UpdatedAt) VALUES
    (N'AbsenceThreshold', N'0.20',        SYSUTCDATETIME()),
    (N'PassingGrade',     N'75',          SYSUTCDATETIME()),
    (N'QrStepSeconds',    N'30',          SYSUTCDATETIME()),
    (N'MaxEmailAttempts', N'5',           SYSUTCDATETIME()),
    (N'TimeZoneId',       N'Asia/Manila', SYSUTCDATETIME());
GO
