# Flowcharts — Milestone 13 Task 5 (Parent portal polish)

## Logic (how each attendance and grade cell is styled)
```mermaid
flowchart TD
    A[Render a record row] --> B{Status = Absent?}
    B -- yes --> C[Bold, danger-emphasis text]
    B -- no --> D[Normal text]
    E[Render a grade row] --> F{Score null?}
    F -- yes --> G[Not graded yet, secondary text]
    F -- no --> H[score / max]
    I[Summary toggle] --> J[padding-block .75rem → 42 px tap target]
```

## Sequence (how the polish was verified)
```mermaid
sequenceDiagram
    participant C as Claude
    participant B as Browser pane (hidden)
    participant W as Website
    participant API as Web API
    C->>B: resize to 375 px, open Details/1
    B->>W: GET /Parent/Children/Details/1
    W->>API: overview + children
    W-->>B: HTML
    C->>B: JS measurements (overflow, heights, contrast, headings)
    C->>C: fix Details.cshtml + site.css, restart site
    C->>B: measure again at 375 / 480 / 1280 px
```

No database change, so there is no ER diagram.
