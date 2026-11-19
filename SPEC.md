# Spec: Demo deploy and demo data

> Status: **Approved** (2026-11-20). Plan: `tasks/plan-demo.md`.
> The previous spec (Failed emails page checks) is in git history (commit `197111e`).
> Source of truth order: `docs/AI_ASPNET_Core_MVC_Development_Guide.md` → `CLAUDE.md` → this spec.

## Objective
Get Klasly ready for the demo on 2026-11-21:
- the API, the MVC portal and the Worker run on the team's Ubuntu VPS behind HTTPS;
- the database starts with a **realistic four-week history**, so the at-risk badges already show a mix when the demo begins;
- the live demo adds to that history and flips one student to **At risk** on screen.

**Users:** the two presenters (admin, teacher and parent roles in the browser; student and parent on the phone) and the panel watching.

## Decisions (answered 2026-11-20)
1. **Demo data:** a one-time SQL script run after the migration, not more `HasData` (that would change the migration and the schema comparison).
2. **VPS access:** Claude generates an SSH key on this PC and the user adds its public half to the VPS. Claude then runs the deploy over SSH and **asks before each step that changes the server**.
3. **Firebase + Brevo:** set up. The user creates the accounts and pastes the Brevo key and the Firebase files. Claude never types third-party secrets.

## Demo data (`deploy/demo-data.sql`)
- **Window:** sessions on the class schedule from 30 to 3 days ago, using Manila dates.
  - That's exactly 4 of each weekday: class 1 (MWF 07:30) gets **12** sessions and class 2 (TTh 13:00) gets **8**.
  - Nothing falls within the Worker's 48-hour absence-alert lookback, so loading the history sends no absence emails.
- **Sessions:** `Finalized`, `ClosedAt = EndTime`, a random `QrNonce`, and times stored in UTC.
- **Attendance:** one record per active enrollment per session.
  - Present/Late come from `Scan`, with `ScannedAt`.
  - Absent comes from `Auto`, with no `ScannedAt`, the same as the Worker writes it.
- **Grades:** posted in the past.
  - Class 1: Quiz 1 (20 pts, 15%), Laboratory 1 (50 pts, 15%), Midterm Exam (100 pts, 30%). That totals 60%, leaving 40% for the live demo.
  - Class 2: Quiz 1 (20 pts, 20%), Midterm Exam (100 pts, 30%).
- **Expected risk result** (thresholds 0.20 / 75 from SystemSettings):

| Class | Student | Absent / counted | Weighted avg | At risk |
|---|---|---|---|---|
| 1 ELECT3 | Aira | 0/12 (one Late) | 89.50 | no |
| 1 | Bryan | 4/12 = 0.3333 | 79.50 | **yes** (absences) |
| 1 | Carla | 1/12 = 0.0833 | 65.75 | **yes** (grades) |
| 1 | Dennis | 2/12 = 0.1667 | 79.25 | no (one more absence flips him) |
| 1 | Ellen | 0/12 (two Late) | 93.25 | no |
| 1 | Francis | 1/12 = 0.0833 | 80.00 | no |
| 2 IT302 | Aira | 0/8 | 85.00 | no |
| 2 | Bryan | 1/8 = 0.1250 | 76.80 | no |
| 2 | Carla | 0/8 (one Late) | 80.00 | no |

- **Safe to run twice:** if the database already has sessions, the script prints a message and changes nothing.

## Deploy (VPS)
Follow `deploy/` (systemd ×3, nginx, env template) and Part B of `docs/reports/2026-11-20-0257-demo-readiness/HOW-TO-USE.md`, with SQL Server 2022 Express in Docker on `127.0.0.1:1433`.

## Success criteria
- [x] `DemoDataTests`: on a freshly migrated test database, the script produces exactly the risk table above, and a second run adds nothing.
- [x] Linux builds of the API, Web and Worker and the migration bundle are published to `publish/` (git-ignored).
- [x] An SSH key exists at `~/.ssh/klasly_vps`, and the user knows the one command that installs it on the VPS.
- [ ] After the deploy: `https://<domain>/api/auth/me` → 401, the login page loads, and Marites's child page shows Aira "On track" and Bryan "At risk".
- [x] The checklist is updated: the demo-data step, the alert email is the parent's **Contact email**, and the live demo flips Dennis.
- [x] `dotnet build` with 0 warnings, and `dotnet test` all green.

## Out of scope
- Building the APK. This PC has no JDK or Android SDK; a teammate builds it with `klasly.apiBaseUrl=https://<domain>/api/`.
- Creating Firebase/Brevo accounts or typing their keys (the user does it).
- Any schema, API or UI change.

## Boundaries
- **Always:** keep the server's secrets only in `/etc/klasly/klasly.env` (`chmod 600`); confirm before each step that changes the server.
- **Ask first:** accepting Let's Encrypt terms (`certbot --agree-tos`), opening firewall ports, anything that deletes data on the server.
- **Never:** commit secrets, expose SQL Server publicly, or type the user's passwords or third-party API keys.
