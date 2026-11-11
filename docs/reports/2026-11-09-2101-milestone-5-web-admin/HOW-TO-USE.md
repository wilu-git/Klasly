# How to use: the Klasly Admin portal

## Prerequisites
- SQL Server LocalDB with the `Klasly` database (Milestone 2 migration + seed).
- A trusted dev HTTPS certificate (one time per PC): `dotnet dev-certs https --trust`
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (https://localhost:7256)
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https` (https://localhost:7144)

## Step by step: set up a term
1. Open `https://localhost:7144` and log in as the admin (`admin@klasly.test`, test password `Klasly@123`). You land on **Admin**.
2. **Terms** → **New term**. Type the name (e.g. "2nd Sem 2026-27"), pick the start and end dates, **Save**. Click **Make active** on the term that is running now. Only one term is active at a time.
3. **Subjects** → **New subject**. Code (e.g. "CPE 401"), title, units (1–10), **Save**.
4. **Accounts** → **New teacher** / **New student** / **New parent** / **New admin**. The form shows only the fields that role needs. Give the password to the person privately.
   - To change details, reset a password or turn an account back on: **Edit**. Leave "New password" blank to keep the old one.
   - **Deactivate** stops that person from logging in. Nothing is deleted.
5. Link a parent to a child: **Accounts** → **Parent** tab → **Edit** on the parent → **Link a student** at the bottom. Choose the student, type the relationship, keep "Receives alerts" on, **Link**.
6. **Classes** → **New class**. Pick the subject, term (the active term is already chosen) and teacher, type the section, schedule and room, **Save**.
7. On the class row click **Students** → choose a student under **Enroll a student** → **Enroll**. **Drop** removes a student from the class but keeps their records; enrolling them again brings them back.

## Example (screen description)
The Classes page shows a **Term** dropdown (the active term is selected) with a **Show** button, then a table: subject code and title, section, teacher, schedule, room, number enrolled, and **Students** / **Edit** buttons. Choosing "All terms" adds a Term column.

If you type something the server refuses (for example a subject code that already exists), the form stays filled in and a red box says exactly what is wrong: "Subject code 'CPE 401' is already used."

## Troubleshooting
| Problem | Fix |
|---|---|
| "Klasly can't reach the server right now." | The Web API isn't running, or the HTTPS certificate isn't trusted. Start the API and run `dotnet dev-certs https --trust`. |
| "Your session ended, please log in again." | Your login lasts as long as the API token (60 minutes). Log in again; you return to the page you were on. |
| A teacher is missing from the class Teacher dropdown | Only active teacher accounts are listed. Reactivate the teacher under **Accounts → Edit**. |
