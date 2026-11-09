# How to use — the Klasly database (Milestone 2)

This milestone has no screens yet. What you can do is create the `Klasly` database from the code
and look at the seeded test data.

## Prerequisites

- .NET 10 SDK (`dotnet --version` should print `10.0.4xx`).
- SQL Server LocalDB (`(localdb)\MSSQLLocalDB`) — it ships with Visual Studio.
- The `dotnet-ef` tool, version 10.x: `dotnet tool install -g dotnet-ef` (or `update` instead of
  `install` if you already have it).
- Run every command from the solution folder (the one with `Klasly.slnx`).

## Step-by-step

1. **Build everything.**

   ```
   dotnet build
   ```

   You should see `Build succeeded` and no warnings.

2. **Create the database.**

   ```
   dotnet ef database update -p src/Klasly.Infrastructure -s src/Klasly.API
   ```

   > **If it says "There is already an object named 'AcademicTerms'"**, your LocalDB still has the
   > old hand-made `Klasly` database from the earlier attempt. It does not match this model, so it
   > has to go. **This deletes that database and everything in it** (it is test data only, but check
   > with the team first):
   >
   > ```
   > dotnet ef database drop -p src/Klasly.Infrastructure -s src/Klasly.API --force
   > ```
   >
   > Then run the `database update` command again.

3. **Look at the data.** In Visual Studio: *View → SQL Server Object Explorer →
   (localdb)\MSSQLLocalDB → Databases → Klasly → Tables*. Right-click a table → *View Data*.
   You should see 17 tables (plus `__EFMigrationsHistory`, which EF uses to track migrations).

4. **Check the seeded rows.** `SystemSettings` has 5 rows; `Users` has 12
   (1 admin, 2 teachers, 6 students, 3 parents); `AcademicTerms` has the one active term
   *1st Sem 2026-27*; `Classes` has 2 classes with 9 enrollments between them.

5. **Adding a column later?** Change the entity and its configuration, then:

   ```
   dotnet ef migrations add <ShortName> -p src/Klasly.Infrastructure -s src/Klasly.API
   ```

   followed by `dotnet ef database update ...`. Never change the tables by hand in SQL Server.

## Example — the seeded test accounts

All fictional (RA 10173). Every one of them uses the same test password: **`Klasly@123`**.

| Role | Email | Notes |
| --- | --- | --- |
| Admin | `admin@klasly.test` | |
| Teacher | `rosa.villanueva@klasly.test` | owns ELECT3 — BSCpE 3-A |
| Teacher | `mark.delossantos@klasly.test` | owns IT302 — BSCpE 3-A |
| Student | `aira.mendoza@klasly.test` … `francis.nuqui@klasly.test` | 6 students, numbers 2026-00001 … 2026-00006 |
| Parent | `marites.mendoza@klasly.test`, `rodel.dimaano@klasly.test`, `linda.bautista@klasly.test` | 2 children each |

Classes: **ELECT3 — Mobile Application Development**, section BSCpE 3-A, all 6 students; and
**IT302 — Software Engineering**, same section, the first 3 students. Term *1st Sem 2026-27*
(17 Aug 2026 – 19 Dec 2026), marked active.

Logging in with these accounts is Milestone 3 — right now they are only rows in the database.

## Troubleshooting

- **"Connection string 'Klasly' is missing."** The API is not running in Development, so
  `appsettings.Development.json` is not read. Set `ASPNETCORE_ENVIRONMENT=Development`, or add the
  same `ConnectionStrings:Klasly` entry to your own user-secrets.
- **"A network-related or instance-specific error… (localdb)\MSSQLLocalDB"** — LocalDB is asleep or
  not installed. Run `sqllocaldb start MSSQLLocalDB`, or install the *Data storage and processing*
  workload in the Visual Studio Installer.
- **"The Entity Framework tools version '10.0.11' is older than that of the runtime '10.0.14'."**
  Only a notice; the commands still work. `dotnet tool update -g dotnet-ef` clears it.
- **"Your startup project 'Klasly.API' doesn't reference Microsoft.EntityFrameworkCore.Design"** —
  someone re-added `<PrivateAssets>all</PrivateAssets>` to that package in
  `Klasly.Infrastructure.csproj`. Remove it; `dotnet ef` needs it to reach the API project.
