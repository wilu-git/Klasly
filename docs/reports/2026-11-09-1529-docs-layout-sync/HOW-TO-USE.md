# HOW-TO-USE — Finding your way around the Klasly docs

Nothing to run this time: this change fixed the project documents so they match the code that is actually in the repo. Here is what moved and what you should do differently from now on.

## Prerequisites

- Nothing running. No SQL Server, no API, no Worker, no emulator.
- To rebuild the solution: the .NET 10 SDK (`dotnet --version` should print `10.0.x`).

## Step-by-step

1. **Read the guide at its real name.** The source of truth is `docs/AI_ASPNET_Core_MVC_Development_Guide.md`. Older notes called it `docs/ai-dev-guide.md`; that file never existed. Section 27 holds the Klasly outline, the 17 tables and the business rules.
2. **Read the schema spec at `docs/database/Klasly_Schema.sql`.** 330 lines, 17 `CREATE TABLE` statements. This is the specification the EF Core model must reproduce — not something you run by hand. The database is built by migrations only.
3. **Put new data-layer files in the right project.** The old docs said one library called `Klasly.Data`. There is no such project. Use this instead:

   | What you are writing | Where it goes |
   |---|---|
   | An entity class (e.g. `ClassSession.cs`) | `src/Klasly.Core/Entities/` |
   | A DTO | `src/Klasly.Core/DTOs/` |
   | A repository *interface* | `src/Klasly.Core/Interfaces/Repositories/` |
   | `KlaslyDbContext`, a Fluent API configuration | `src/Klasly.Infrastructure/Data/`, `Data/Configurations/` |
   | A repository *implementation* | `src/Klasly.Infrastructure/Repositories/` |

4. **Use the new migration commands.** Both the project and the startup project changed name:

   ```bash
   dotnet ef migrations add <Name> -p src/Klasly.Infrastructure -s src/Klasly.API
   ```

   ```bash
   dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API
   ```

5. **Check it still builds.** The solution file is `Klasly.slnx` (the new XML format), not `Klasly.sln`:

   ```bash
   dotnet build Klasly.slnx
   ```

   Expect `Build succeeded. 0 Warning(s), 0 Error(s)` and five projects: Core, Infrastructure, API, Web, Worker.

## Example

Before (in `CLAUDE.md`, describing a project that does not exist):

```text
├── Klasly.Data/           ← ENTITY FRAMEWORK CORE + MSSQL
```

After (describing the projects that are really there):

```text
├── Klasly.Core/           ← DOMAIN: entities, DTOs, interfaces, business-rule services
├── Klasly.Infrastructure/ ← ENTITY FRAMEWORK CORE + MSSQL (DbContext, configs, migrations, repositories)
```

## Troubleshooting

**"`dotnet ef` says it cannot find the project `src/Klasly.Data`."**
You copied an old command. There is no `Klasly.Data`. Use `-p src/Klasly.Infrastructure -s src/Klasly.API`.

**"`dotnet ef` says no DbContext was found."**
Expected right now. On this branch `src/Klasly.Infrastructure` has no EF Core packages and no `Data/KlaslyDbContext.cs` yet — that is milestone 2. Nothing is broken.

**"`dotnet build Klasly.sln` says the file does not exist."**
The solution is `Klasly.slnx`. Build the folder instead (`dotnet build`) or name the `.slnx` file.

**"Visual Studio shows CLAUDE.md as conflicted / wants me to discard changes."**
That conflict is resolved and the file is staged. Do **not** use "Discard changes" on `CLAUDE.md` — it carries edits that are not in any commit yet.
