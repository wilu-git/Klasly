# Flowcharts — Milestone 12 Task 1 (Brevo email client)

## Client logic

```mermaid
flowchart TD
    A[SendAsync to, name, subject, text] --> B[POST smtp/email<br/>api-key header + JSON]
    B --> C{What happened?}
    C -- caller cancelled --> X[throw OperationCanceledException]
    C -- HttpClient timeout --> T1[Transient: timeout]
    C -- network error --> T2[Transient: network error]
    C -- response --> D{Status}
    D -- 2xx --> E[Accepted + messageId<br/>unreadable body → '(no messageId)']
    D -- 401 / 403 --> U[Unauthorized: status + body]
    D -- 429 / 5xx --> T3[Transient: status + body]
    D -- other 4xx --> P[Permanent: status + body]
```

## Where it will sit (after Task 5)

```mermaid
sequenceDiagram
    participant J as WorkerTickJob
    participant S as EmailSender (Task 2)
    participant C as BrevoEmailClient (this task)
    participant B as Brevo API v3
    participant DB as MSSQL
    J->>S: SendDueAsync(now)
    S->>DB: up to 50 due Email rows
    loop each row
        S->>C: SendAsync(guardian email, name, subject, body)
        C->>B: POST /v3/smtp/email (10 s timeout)
        B-->>C: 201 / 4xx / 5xx / no answer
        C-->>S: EmailResult(outcome, messageId, error)
        S->>DB: update the row, save
    end
```

No database change in this task, so no ER diagram.
