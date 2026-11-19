# How to use the demo data

## Prerequisites
- A fresh `Klasly` database created by the migration bundle (on the VPS) or by `dotnet ef database update` (locally).
- The Worker, which computes the at-risk badges after the data is loaded.

## Step by step
1. Create the database, then run `deploy/demo-data.sql` once:
   - **VPS (Docker SQL Server):**
     ```bash
     sudo docker exec -i klasly-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<sa password>' -d Klasly -b < ~/deploy/demo-data.sql
     ```
   - **Locally:** open the file in SSMS or VS Code (mssql extension) against `(localdb)\MSSQLLocalDB` / `Klasly` and run it.
2. Start the Worker. Within one tick, the risk flags exist.
3. Log in as Marites (parent): Aira shows **On track**, Bryan shows **At risk**. Rodel sees Carla **At risk** and Dennis **On track**.
4. Rosa (teacher) sees 12 finished ELECT3 sessions and 3 graded assessments on the class page.

The full demo checklist is `docs/reports/2026-11-20-0257-demo-readiness/HOW-TO-USE.md`.

## Example
`sqlcmd` prints: `Demo data loaded: 20 finalized sessions, their attendance, 5 assessments and 24 grades. Start the Worker to compute the risk flags.`

## Troubleshooting
- **"Demo data skipped: this database already has sessions or assessments":** the database isn't fresh. Locally: `dotnet ef database drop -p src/Klasly.Infrastructure -s src/Klasly.API --force`, then `database update`, then run the script again.
- **The parent page says "Not computed yet":** the Worker hasn't run since the data was loaded. Check `journalctl -u klasly-worker -n 50`.
- **The sessions are on unexpected dates:** they're always 3 to 30 days before the day you run the script. Load it the day before the demo, not weeks earlier.
