# How to use: Teacher "My classes" page

## Prerequisites
- SQL Server LocalDB with the `Klasly` database migrated and seeded (Milestone 2).
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (https://localhost:7256).
- The website running: `dotnet run --project src/Klasly.Web --launch-profile https` (https://localhost:7144).

## Step by step
1. Open https://localhost:7144. You are sent to the login page.
2. Log in as a teacher, for example `rosa.villanueva@klasly.test`, using the seed test password from the Milestone 5 how-to.
3. You land on **My classes**. The navbar shows a **My classes** link.
4. Each row is one of *your* classes, with classes from the active term first. Other teachers' classes are never listed.
5. **Sessions** opens the class's session page. That page is added in the next task, so for now it shows "not found".

## Example (screen description)
A heading "My classes", then a table:

| Subject | Section | Term | Schedule | Room | Enrolled | |
|---|---|---|---|---|---|---|
| **ELECT3** Mobile Application Development | BSCpE 3-A | 1st Sem 2026-27 | MWF 07:30-09:00 | CL-301 | 6 | [Sessions] |

A teacher with no classes sees: "You have no classes yet. Ask the admin to assign you to a class."

## Troubleshooting
- **"Klasly can't reach the server right now"** right after starting everything: LocalDB was still waking up and the first call took more than 15 s. Click **Try again**.
- **"Access denied" on /Teacher/Classes**: you are logged in as an Admin or a Parent. Log out and log in with a teacher account.
- **The class you expect is missing**: an Admin must assign you as that class's teacher (Admin → Classes → Edit).
