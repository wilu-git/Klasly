# Flowcharts — Milestone 14

## How a request is checked (what the role and ownership tests prove)

```mermaid
flowchart TD
    A[Request to /api/...] --> B{Endpoint has AllowAnonymous?<br/>only POST /api/auth/login}
    B -- yes --> OK[Controller runs]
    B -- no --> C{Valid JWT?<br/>fallback policy}
    C -- no --> E401[401 Session expired – please log in]
    C -- yes --> D{Role policy on endpoint?}
    D -- "AdminOnly / TeacherOnly / StudentOnly / ParentOnly" --> F{Token role matches?}
    F -- no --> E403a[403 empty body]
    F -- yes --> G[Service]
    D -- "none: auth/me, attendance/scan" --> G
    G --> H{Owner / linked / enrolled?}
    H -- no --> E403b[403 with message<br/>e.g. This class belongs to another teacher.]
    H -- yes --> OK
```

## Sequence: role sweep test → in-memory API → test database

```mermaid
sequenceDiagram
    participant T as RoleAccessTests
    participant F as ApiFactory (in-memory Web API)
    participant S as Services + EF Core
    participant DB as LocalDB Klasly_Tests
    T->>F: POST api/auth/login (seeded account)
    F->>S: AuthService
    S->>DB: user + password hash
    F-->>T: JWT (cached per account)
    loop every controller endpoint (EndpointDataSource)
        T->>F: request with no token / each role's token
        F-->>T: 401 / 403 / allowed
    end
    T->>T: collect failures, Assert.Empty
```

## Sequence: admin opens Failed emails

```mermaid
sequenceDiagram
    participant B as Admin browser
    participant W as Web MVC (Admin/FailedEmails)
    participant A as Web API (NotificationsController)
    participant DB as SQL Server
    B->>W: GET /Admin/FailedEmails (cookie)
    W->>A: GET api/notifications/failed (Bearer JWT)
    A->>A: AdminOnly policy
    A->>DB: Notifications WHERE Channel=Email AND Status=Failed ORDER BY id DESC TOP 100
    DB-->>A: rows + guardian ContactEmail
    A-->>W: List<FailedEmailResponse>
    W-->>B: table in Manila time, or "No failed emails"
```

No database schema change, so no ER diagram.
