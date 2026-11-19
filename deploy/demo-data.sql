-- Klasly demo history: four weeks of finished class sessions, attendance and grades for the seeded classes.
-- Run ONCE, right after the first migration (efbundle). Fictional data only (RA 10173). Spec: SPEC.md "Demo data".
--   sqlcmd: docker exec -i klasly-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$SA" -d Klasly -b < demo-data.sql
-- The Worker turns this into RiskFlags on its next tick: Bryan (absences) and Carla (grades) at risk in ELECT3,
-- Dennis one absence away. One batch, no GO, so the test can run it through EF too.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM dbo.ClassSessions) OR EXISTS (SELECT 1 FROM dbo.Assessments)
BEGIN
    PRINT 'Demo data skipped: this database already has sessions or assessments. Run it only on a fresh database.';
    RETURN;
END;

-- Manila dates 30 to 3 days ago: exactly 4 of each weekday, and older than the Worker's 48 h absence-alert lookback.
DECLARE @today date = CAST(DATEADD(hour, 8, SYSUTCDATETIME()) AS date);
DECLARE @sessions TABLE (ClassId int, Idx int, StartTime datetime2(0), EndTime datetime2(0));

WITH days AS (
    SELECT TOP (28) DATEADD(day, -(ROW_NUMBER() OVER (ORDER BY (SELECT NULL)) + 2), @today) AS d
    FROM sys.all_objects
),
slots AS (
    -- DATEDIFF from 1900-01-01 (a Monday) mod 7: 0 Mon … 4 Fri. Start times are Manila minutes after midnight.
    SELECT 1 AS ClassId, d, 450 AS StartMin FROM days WHERE DATEDIFF(day, '19000101', d) % 7 IN (0, 2, 4) -- ELECT3 MWF 07:30
    UNION ALL
    SELECT 2, d, 780 FROM days WHERE DATEDIFF(day, '19000101', d) % 7 IN (1, 3)                         -- IT302 TTh 13:00
)
INSERT @sessions (ClassId, Idx, StartTime, EndTime)
SELECT ClassId,
       ROW_NUMBER() OVER (PARTITION BY ClassId ORDER BY d),
       DATEADD(minute, StartMin - 480, CAST(d AS datetime2(0))),      -- Manila (UTC+8) → UTC
       DATEADD(minute, StartMin - 480 + 90, CAST(d AS datetime2(0)))
FROM slots;

-- Everyone not listed here is Present in every session.
DECLARE @exceptions TABLE (ClassId int, StudentId int, Idx int, Status nvarchar(20));
INSERT @exceptions (ClassId, StudentId, Idx, Status) VALUES
    (1, 2, 3, N'Absent'), (1, 2, 6, N'Absent'), (1, 2, 9, N'Absent'), (1, 2, 11, N'Absent'), -- Bryan 4/12
    (1, 3, 7, N'Absent'),                                                                    -- Carla 1/12
    (1, 4, 4, N'Absent'), (1, 4, 10, N'Absent'),                                             -- Dennis 2/12
    (1, 6, 8, N'Absent'),                                                                    -- Francis 1/12
    (1, 1, 2, N'Late'), (1, 5, 5, N'Late'), (1, 5, 12, N'Late'),                             -- Aira, Ellen
    (2, 2, 5, N'Absent'),                                                                    -- Bryan 1/8
    (2, 3, 1, N'Late');                                                                      -- Carla

DECLARE @scores TABLE (ClassId int, Title nvarchar(100), StudentId int, Score decimal(6,2));
INSERT @scores (ClassId, Title, StudentId, Score) VALUES
    (1, N'Quiz 1', 1, 18), (1, N'Quiz 1', 2, 16), (1, N'Quiz 1', 3, 13), (1, N'Quiz 1', 4, 15), (1, N'Quiz 1', 5, 19), (1, N'Quiz 1', 6, 16),
    (1, N'Laboratory 1', 1, 46), (1, N'Laboratory 1', 2, 40), (1, N'Laboratory 1', 3, 33),
    (1, N'Laboratory 1', 4, 41), (1, N'Laboratory 1', 5, 47), (1, N'Laboratory 1', 6, 39),
    (1, N'Midterm Exam', 1, 88), (1, N'Midterm Exam', 2, 79), (1, N'Midterm Exam', 3, 66),
    (1, N'Midterm Exam', 4, 80), (1, N'Midterm Exam', 5, 92), (1, N'Midterm Exam', 6, 81),
    (2, N'Quiz 1', 1, 17), (2, N'Quiz 1', 2, 15), (2, N'Quiz 1', 3, 16),
    (2, N'Midterm Exam', 1, 85), (2, N'Midterm Exam', 2, 78), (2, N'Midterm Exam', 3, 80);

BEGIN TRANSACTION;

INSERT dbo.ClassSessions (ClassId, StartTime, EndTime, LateAfterMinutes, QrNonce, Status, ClosedAt)
SELECT ClassId, StartTime, EndTime, 15,
       CONVERT(nvarchar(64), HASHBYTES('SHA2_256', CAST(NEWID() AS nvarchar(36))), 2), -- 64 hex chars, random per row
       N'Finalized', EndTime
FROM @sessions;

-- Scans look like the API's (Scan + ScannedAt; Late after the 15-minute cut-off), absences like the Worker's (Auto).
INSERT dbo.AttendanceRecords (SessionId, StudentId, Status, ScannedAt, MarkedAt, Source)
SELECT cs.SessionId, e.StudentId, st.Status,
       CASE WHEN st.Status = N'Absent' THEN NULL ELSE scan.At END,
       CASE WHEN st.Status = N'Absent' THEN cs.EndTime ELSE scan.At END,
       CASE WHEN st.Status = N'Absent' THEN N'Auto' ELSE N'Scan' END
FROM @sessions s
JOIN dbo.ClassSessions cs ON cs.ClassId = s.ClassId AND cs.StartTime = s.StartTime
JOIN dbo.Enrollments e ON e.ClassId = s.ClassId AND e.Status = N'Active'
CROSS APPLY (SELECT COALESCE((SELECT x.Status FROM @exceptions x
                              WHERE x.ClassId = s.ClassId AND x.StudentId = e.StudentId AND x.Idx = s.Idx), N'Present') AS Status) st
CROSS APPLY (SELECT DATEADD(minute, CASE st.Status WHEN N'Late' THEN 22 ELSE 4 END, cs.StartTime) AS At) scan;

-- 60% of ELECT3 and 50% of IT302 graded, so the live demo can still add an assessment.
INSERT dbo.Assessments (ClassId, Title, Type, MaxScore, WeightPercent, DatePosted) VALUES
    (1, N'Quiz 1',       N'Quiz',     20,  15, DATEADD(day, -24, SYSUTCDATETIME())),
    (1, N'Laboratory 1', N'Activity', 50,  15, DATEADD(day, -17, SYSUTCDATETIME())),
    (1, N'Midterm Exam', N'Exam',     100, 30, DATEADD(day, -6,  SYSUTCDATETIME())),
    (2, N'Quiz 1',       N'Quiz',     20,  20, DATEADD(day, -20, SYSUTCDATETIME())),
    (2, N'Midterm Exam', N'Exam',     100, 30, DATEADD(day, -5,  SYSUTCDATETIME()));

INSERT dbo.Grades (AssessmentId, StudentId, Score, PostedAt)
SELECT a.AssessmentId, sc.StudentId, sc.Score, a.DatePosted
FROM @scores sc
JOIN dbo.Assessments a ON a.ClassId = sc.ClassId AND a.Title = sc.Title;

COMMIT;

PRINT 'Demo data loaded: 20 finalized sessions, their attendance, 5 assessments and 24 grades. Start the Worker to compute the risk flags.';
