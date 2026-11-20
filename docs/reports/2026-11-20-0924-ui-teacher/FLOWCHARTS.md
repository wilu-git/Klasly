# Flowcharts — ui-teacher

```mermaid
flowchart TD
  A[Teacher opens class page] --> B{A session IsLive?}
  B -- yes --> C[Live strip + Show QR on top]
  C --> D[Start form inside 'Start another session' details]
  B -- no --> E[Start form card on top]
  D --> F[Teacher posts Start]
  E --> F
  F --> G{API error?}
  G -- no --> H[Redirect to session page]
  G -- yes --> I[AddApiErrors, MoveSummaryErrorsTo 'start']
  I --> J[Class page again: error only in Start form, details open]
  K[Teacher posts Add assessment] --> L{API error?}
  L -- yes --> M[MoveSummaryErrorsTo 'assessment' → error only in Add form]
  L -- no --> N[Redirect to new grade sheet]
```

```mermaid
sequenceDiagram
  participant Teacher
  participant MVC as Klasly.Web
  participant API as Klasly.API
  Teacher->>MVC: POST /Teacher/Sessions/Correct/1?studentId=4 (status, currentStatus)
  MVC->>API: PUT attendance correction
  API-->>MVC: 200 row
  MVC-->>Teacher: 302 /Teacher/Sessions/Details/1#row-4 (+ flash)
  Teacher->>MVC: GET /Teacher/Sessions/Details/1
  MVC-->>Teacher: page; browser scrolls to tr#row-4
```
