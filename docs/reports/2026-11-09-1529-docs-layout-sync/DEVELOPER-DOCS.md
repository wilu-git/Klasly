# DEVELOPER-DOCS — Docs/layout sync (Core + Infrastructure)

## Summary

The repo's real project layout (`Klasly.Core` + `Klasly.Infrastructure`) had drifted from the docs, which still described a single `Klasly.Data` library. We kept the real layout and corrected the documents instead of renaming projects. Also recovered two reference documents that were missing from the `master` working tree: the AI development guide and `Klasly_Schema.sql`. No application code was touched.

## Projects touched

None. This change is documentation only — no WEB MVC / WEB API / EF CORE / MSSQL / CONSOLE WORKER / ANDROID KOTLIN source files were modified.

## Files changed

| path | added/modified/deleted | why |
|---|---|---|
| `CLAUDE.md` | modified (+ conflict resolved) | Section 0 table, Section 3 tree, Section 3.2, 3.4 and milestone 2 now describe `Klasly.Core` + `Klasly.Infrastructure`, the real guide filename, `Klasly.slnx` and `Klasly.API` casing. The file was in an unmerged `DU` state after a branch switch; resolved in favour of the working copy. |
| `docs/AI_ASPNET_Core_MVC_Development_Guide.md` | added (restored) | Declared the source of truth by CLAUDE.md Section 0, but absent from `master`. Restored from commit `2dfd15e`, then Section 2's project note, Section 24's example tree and Section 27.4 were updated to the two-library layout. |
| `docs/database/Klasly_Schema.sql` | added (restored) | Reference specification for the EF Core model (17 tables). Restored from commit `71d4a14` to the path CLAUDE.md Section 0 points at. |
| `docs/reports/2026-11-09-1529-docs-layout-sync/*` | added | This report (CLAUDE.md Section 5). |
| `docs/reports/INDEX.md` | added | Report index (CLAUDE.md Section 5). |

## Key code

No code. The decision now recorded in `CLAUDE.md` Section 3.2:

```text
### 3.2 DOMAIN + ENTITY FRAMEWORK CORE — `Klasly.Core` and `Klasly.Infrastructure`
The shared data layer is **two** class libraries, not one. This overrides guide
Section 27.4, which named a single `Klasly.Data`: that project was never created,
and the existing split takes priority (guide Section 20).
```

Where data-layer files go from now on:

| Concern | Project / folder |
|---|---|
| Entities, DTOs, interfaces | `src/Klasly.Core/Entities`, `DTOs/`, `Interfaces/` |
| `KlaslyDbContext`, Fluent API configs, migrations | `src/Klasly.Infrastructure/Data`, `Data/Configurations/`, `Data/Migrations/` |
| EF Core repositories | `src/Klasly.Infrastructure/Repositories` |
| `AddInfrastructure()` | `src/Klasly.Infrastructure/DependencyInjection.cs` |

The migration commands changed in both documents:

```bash
dotnet ef migrations add <Name> -p src/Klasly.Infrastructure -s src/Klasly.API
dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API
```

## Database changes

None. No migration was added and no table or column changed.

## How it was verified

| Command | Result |
|---|---|
| `dotnet build Klasly.slnx` | **Build succeeded. 0 Warning(s), 0 Error(s)** (39.8 s) — all five projects compiled |
| `grep "Klasly.Data\|Klasly.Api\b"` over both documents | 1 deliberate mention left (the "never created — do not add it" note in guide 27.4); no stale `Klasly.Api` casing |
| Edit script with a per-pattern match-count assertion | 14 of 14 replacements applied exactly once |

## Known limits / TODO

- **`master` is behind `backendDev` in `src/`** by five files: `Klasly.API/Program.cs` still contains the `/weatherforecast` template and does not call `AddInfrastructure()`; `Klasly.API.csproj` lacks `Microsoft.EntityFrameworkCore.Design` and a `UserSecretsId`; `Klasly.Infrastructure.csproj` has **no EF Core packages**; `DependencyInjection` is an empty non-static class; `Data/KlaslyDbContext.cs` does not exist. Milestone 2 cannot start until these exist.
- `docs/klasly-brainstorm.md` is referenced by CLAUDE.md but exists in no commit on any branch. The row now says so.
- `global.json` (CLAUDE.md Section 4.1, milestone 1) does not exist. The installed SDK is 10.0.403, so builds work, but the SDK is not pinned.
- `android/KlaslyApp/` (milestone 1) does not exist.
- On `master`, `docs/` also holds the pre-reorganisation copies: `docs/Klasly_Schema.sql` (byte-identical to the restored `docs/database/` copy), `docs/Klasly ERD.dbml` and five numbered diagram PNGs. CLAUDE.md points at `docs/database/` and `docs/diagrams/`, so those paths are half-true on this branch.
- Nothing is committed. `CLAUDE.md` is staged; the restored documents and this report are untracked.
