# Flowcharts — Milestone 12 review fixes

## How a Brevo answer is sorted (after the fix)

```mermaid
flowchart TD
    A[Brevo answer] --> B{Status}
    B -- 2xx --> ACC[Accepted]
    B -- "401 / 402 / 403" --> UN[Unauthorized:<br/>stop batch, rows stay Pending,<br/>error logged]
    B -- 400 --> C{body code}
    C -- "not_enough_credits /<br/>account_under_validation" --> UN
    C -- other --> P[Permanent: Failed]
    B -- "429 / 5xx" --> T[Transient: retry with backoff]
    B -- "other 4xx" --> P
    B -- "timeout / network" --> T
```

## Worker startup with the Brevo base URL

```mermaid
flowchart TD
    S[Worker starts] --> R[Read Brevo:BaseUrl<br/>missing → default]
    R --> N[Add trailing '/']
    N --> V{Absolute https URL?}
    V -- no --> X[throw 'Brevo:BaseUrl is invalid'<br/>Worker stops before any tick]
    V -- yes --> H[Typed HttpClient BaseAddress<br/>…/v3/ + smtp/email]
```

## Setup flow (Task 6)

```mermaid
sequenceDiagram
    participant T as Team member
    participant BR as Brevo website
    participant US as user-secrets (local)
    participant DB as Klasly dev DB
    participant W as Worker
    participant API as Brevo API
    T->>BR: sign up, confirm email, profile
    T->>BR: add sender, confirm it
    T->>BR: generate API key (xkeysib-…)
    T->>US: Brevo:ApiKey, Brevo:SenderEmail
    T->>DB: guardians → team inboxes; insert fresh absence
    T->>W: dotnet run (1-min cron)
    W->>DB: queue alert
    W->>API: POST /v3/smtp/email
    API-->>W: 201 messageId
    W->>DB: Accepted + messageId
    API-->>T: email in inbox
```

No schema change, so no ER diagram.
