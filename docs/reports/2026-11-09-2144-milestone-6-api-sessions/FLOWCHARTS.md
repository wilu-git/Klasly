# Flowcharts: Milestone 6 QR check-in

## Scan logic (guide §27.8 order)

```mermaid
flowchart TD
    A[Student app: POST /api/attendance/scan<br/>qrToken + deviceId] --> B{JWT valid,<br/>role Student,<br/>account active?}
    B -- no --> E1[401 Session expired – please log in]
    B -- yes --> C{Token has 3 parts,<br/>session exists,<br/>HMAC matches?}
    C -- no --> E2[400 Invalid QR code]
    C -- yes --> D{Step = current<br/>or previous?}
    D -- no --> E3[400 QR expired – rescan]
    D -- yes --> F{Status Open and<br/>now before EndTime?}
    F -- no --> E4[409 Session is closed]
    F -- yes --> G{Active enrollment<br/>in the class?}
    G -- no --> E5[403 Not enrolled in this class]
    G -- yes --> H{deviceId equals<br/>RegisteredDeviceId?}
    H -- no / none --> E6[403 Unregistered device]
    H -- yes --> I{Record already<br/>exists?}
    I -- yes --> E7[409 Already checked in]
    I -- no --> J{now ≤ Start + LateAfterMinutes?}
    J -- yes --> K[Insert Present]
    J -- no --> L[Insert Late]
    K --> M{Insert won the race?}
    L --> M
    M -- no, unique index --> E7
    M -- yes --> N[200 status, scannedAtUtc, subject, section]
```

## Device registration and teacher session flow

```mermaid
flowchart TD
    S1[Student logs in on the app] --> S2[POST /api/students/me/device]
    S2 --> S3{Phone registered?}
    S3 -- none --> S4[Save this phone · 200]
    S3 -- same phone --> S5[No change · 200]
    S3 -- other phone --> S6[409 Ask the admin to reset it]
    S6 -. admin .-> S7[DELETE /api/accounts/id/device · 204] --> S1

    T1[Teacher: POST classes/id/sessions] --> T2{End after start<br/>and in the future?}
    T2 -- no --> T3[400]
    T2 -- yes --> T4[Scheduled + random nonce]
    T4 --> T5[POST sessions/id/open → Open]
    T5 --> T6[GET sessions/id/qr every 30 s]
    T6 --> T7[POST sessions/id/close → Closed]
```

## Sequence across the layers

```mermaid
sequenceDiagram
    participant T as Teacher (Swagger / M7 page)
    participant A as Android app (M8)
    participant C as Controllers (Klasly.API)
    participant S as Services
    participant R as Repositories (Klasly.Infrastructure)
    participant DB as SQL Server

    T->>C: POST /api/sessions/{id}/open (JWT Teacher)
    C->>S: SessionService.OpenAsync
    S->>R: ISessionRepository.GetByIdAsync + ownership
    R->>DB: SELECT ClassSessions JOIN Classes
    S->>R: SaveChangesAsync (Status = Open)
    T->>C: GET /api/sessions/{id}/qr
    C->>S: GetQrAsync → QrTokenService.Create (HMAC, nonce, step)
    S-->>T: { token, expiresAtUtc, stepSeconds }

    A->>C: POST /api/students/me/device (JWT Student)
    C->>S: AccountService.RegisterDeviceAsync
    S->>R: IUserRepository.GetByIdAsync / SaveChangesAsync
    R->>DB: UPDATE Users SET RegisteredDeviceId

    A->>C: POST /api/attendance/scan { qrToken, deviceId }
    C->>S: AttendanceService.ScanAsync (checks 1–7)
    S->>R: users, sessions, enrollments, attendance.ExistsAsync
    R->>DB: SELECTs
    S->>R: IAttendanceRepository.TryAddAsync
    R->>DB: INSERT AttendanceRecords (unique SessionId+StudentId)
    S-->>A: 200 Present / Late (or the check's error)
    T->>C: GET /api/sessions/{id}/attendance
    C-->>T: roster rows (status null until scanned)
```

No `erDiagram`: the database did not change.
