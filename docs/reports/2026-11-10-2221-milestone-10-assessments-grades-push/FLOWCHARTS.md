# Flowcharts: assessments, grade entry and the grade push

## Grade save and push logic

```mermaid
flowchart TD
    A[Teacher presses Save grades] --> B[MVC posts the sheet, anti-forgery]
    B --> C[PUT api/assessments/id/grades]
    C --> O{Own class?}
    O -- no --> E403[403]
    O -- yes --> V[Check every row:<br/>active student, not twice,<br/>0 ≤ score ≤ max, ≤ 2 decimals]
    V -- any error --> E400[400 errors grades i .score<br/>nothing saved, shown on rows]
    V -- all fine --> L[For each non-blank row]
    L --> X{Grade exists?}
    X -- yes, same score --> L
    X -- yes, different --> U[Update Score, UpdatedAt]
    X -- no --> N[Insert grade + Pending push row<br/>DedupKey grade:a:s]
    U --> S
    N --> S[One SaveChanges<br/>+ DatePosted on first grade]
    S -- unique clash --> E409[409 Reload and try again]
    S -- saved --> Q[Enqueue new push ids] --> R[200 added / changed]
    Q --> P{PushSender, background}
    P -- not configured --> F1[Failed: FCM is not configured]
    P -- no token --> F2[Failed: No device registered]
    P -- FCM 200 --> OK[Accepted + message name]
    P -- FCM error --> F3[Failed + reason]
```

## Sequence across the layers

```mermaid
sequenceDiagram
    participant B as Teacher browser
    participant M as Web MVC
    participant A as Web API
    participant DB as MSSQL Klasly
    participant Q as PushQueue / PushSender
    participant G as Google OAuth + FCM
    participant P as Android app

    P->>A: PUT api/students/me/fcm-token (after student login / new token)
    A->>DB: clear token from others, set on caller
    B->>M: POST Teacher/Assessments/SaveGrades
    M->>A: PUT api/assessments/{id}/grades (JWT)
    A->>DB: INSERT/UPDATE Grades + INSERT Notifications (one transaction)
    A->>Q: enqueue new notification ids
    A-->>M: 200 { added, changed, grades }
    M-->>B: redirect + "Grades saved: X new, Y changed."
    Q->>DB: load Pending push + user token
    Q->>G: JWT → access token (cached), POST messages:send
    G-->>P: notification "New grade posted"
    Q->>DB: Accepted / Failed
```

No `erDiagram`: the database did not change (Assessments, Grades, Notifications and Users.FcmToken already existed).
