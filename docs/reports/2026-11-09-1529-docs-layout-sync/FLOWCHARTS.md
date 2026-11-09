# FLOWCHARTS — Docs/layout sync

## Where does a new data-layer file go?

```mermaid
flowchart TD
    A[New file for the data layer] --> B{What is it?}
    B -->|Entity, DTO or interface| C[src/Klasly.Core]
    B -->|DbContext, Fluent API config, migration| D[src/Klasly.Infrastructure/Data]
    B -->|Repository implementation| E[src/Klasly.Infrastructure/Repositories]
    B -->|Business rule / validation| F[Service in Klasly.API or Klasly.Worker]
    B -->|Razor view or ViewModel| G[src/Klasly.Web]

    C --> H{Does Klasly.Web need it?}
    D --> H
    E --> H
    H -->|Yes| I[STOP: Web must not reference Core or Infrastructure]
    I --> J[Expose it as an API endpoint + DTO instead]
    H -->|No| K{Does it match docs/database/Klasly_Schema.sql?}
    K -->|No| L[STOP: fix the model, not the database]
    K -->|Yes| M[Write it, then dotnet build Klasly.slnx]
    F --> M
    G --> M
    M --> N{Build succeeded?}
    N -->|No| L
    N -->|Yes| O[Write the report in docs/reports/]
```

## How a migration reaches the database

The startup project (`Klasly.API`) supplies the connection string and the DI container; the migrations themselves live in `Klasly.Infrastructure`. Both names changed in this update.

```mermaid
sequenceDiagram
    actor Dev as Developer
    participant CLI as dotnet ef (10.x)
    participant API as Klasly.API (startup project)
    participant Infra as Klasly.Infrastructure
    participant DB as MSSQL (Klasly)

    Dev->>CLI: migrations add <Name> -p src/Klasly.Infrastructure -s src/Klasly.API
    CLI->>API: build, read appsettings + user-secrets
    API->>Infra: AddInfrastructure() -> KlaslyDbContext
    Infra-->>CLI: model snapshot
    CLI->>Infra: write Data/Migrations/<ts>_<Name>.cs
    Dev->>CLI: database update -p src/Klasly.Infrastructure -s src/Klasly.API
    CLI->>DB: apply migration (DDL)
    DB-->>CLI: __EFMigrationsHistory updated
    Dev->>CLI: migrations script
    CLI-->>Dev: SQL to compare with docs/database/Klasly_Schema.sql
```

No `erDiagram` this time — the database did not change.
