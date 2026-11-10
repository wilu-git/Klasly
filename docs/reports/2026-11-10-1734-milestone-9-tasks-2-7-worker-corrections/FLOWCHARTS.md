# Flowcharts: Worker finalize + attendance corrections

## Worker tick and correction logic

```mermaid
flowchart TD
    T[Hangfire fires WorkerTickJob<br/>cron Worker:Cron] --> N[now = UtcNow]
    N --> E[List Open/Scheduled sessions<br/>with EndTime ≤ now]
    E --> O{Status?}
    O -- Open --> C[Closed, ClosedAt = now]
    O -- Scheduled --> X[Cancelled<br/>never opened]
    C --> S1[Save]
    X --> S1
    S1 --> L[List Closed sessions]
    L --> R[Read records + Active enrollments]
    R --> A[Add Absent / Auto<br/>for each student without a record]
    A --> F[Status = Finalized]
    F --> TS{TrySaveAsync}
    TS -- saved --> L
    TS -- duplicate key 2601/2627 --> W[Log warning, stop this tick<br/>next tick re-reads]

    U[Teacher picks a status + Save] --> P[PUT api/sessions/id/attendance/studentId]
    P --> V{Valid status?}
    V -- no --> E400[400]
    V -- yes --> OWN{Own class?}
    OWN -- no --> E403[403]
    OWN -- yes --> ENDED{Closed or Finalized?}
    ENDED -- no --> E409[409 only after the session has ended]
    ENDED -- yes --> ENR{Actively enrolled?}
    ENR -- no --> E404[404]
    ENR -- yes --> SAME{Same status?}
    SAME -- yes --> OK[200, nothing written]
    SAME -- no --> WR[Update or insert<br/>Source Manual, MarkedBy, MarkedAt]
    WR --> RACE{Insert lost a race?}
    RACE -- yes --> E409b[409 Reload and try again]
    RACE -- no --> LOG[Log old → new] --> OK2[200 updated row]
```

## Sequence across the layers

```mermaid
sequenceDiagram
    participant H as Hangfire (Worker)
    participant J as WorkerTickJob
    participant F as AttendanceFinalizer
    participant Rp as Repositories (EF Core)
    participant DB as MSSQL Klasly
    participant B as Teacher browser
    participant M as Web MVC
    participant A as Web API

    H->>J: RunAsync(ct) every tick
    J->>F: CloseEndedSessionsAsync(now)
    F->>Rp: ListEndedAsync(now)
    Rp->>DB: SELECT ... WHERE Status IN (Open, Scheduled) AND EndTime <= now
    F->>Rp: SaveChanges (Closed / Cancelled)
    J->>F: MarkAbsentAndFinalizeAsync(now)
    loop each Closed session
        F->>Rp: records + enrollments
        F->>Rp: Add Absent rows, Status = Finalized
        F->>Rp: TrySaveAsync (one transaction)
        Rp->>DB: INSERT AttendanceRecords; UPDATE ClassSessions
    end

    B->>M: POST Teacher/Sessions/Correct (anti-forgery)
    M->>A: PUT api/sessions/{id}/attendance/{studentId} (JWT)
    A->>Rp: own session? enrolled? record
    Rp->>DB: UPDATE / INSERT AttendanceRecords
    A-->>M: 200 SessionAttendanceRow (or 400/403/404/409)
    M-->>B: Redirect + "Attendance updated for …" (or the API's message)
```

No `erDiagram`: the database did not change.
