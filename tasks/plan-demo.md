# Plan: Demo deploy and demo data

> Built from `SPEC.md` (2026-11-20). One commit per task. Tasks 1–3 run on this PC only; Tasks 4–5 change the VPS, so Claude asks before each server step.

## Task 1 — Demo-data script with its test (TDD)
- **RED:** `tests/Klasly.Tests/DemoDataTests.cs`
  - creates and migrates its own `Klasly_DemoData_Tests` database (so the other tests' rows can't change the numbers);
  - runs `deploy/demo-data.sql`;
  - computes each enrollment through `RiskRepository.ListInputsAsync` + `RiskRules.Compute` with the seeded settings;
  - asserts the spec's table;
  - runs the script again and asserts the row counts don't change.
- **GREEN:** `deploy/demo-data.sql`, one T-SQL batch (no `GO`), so it runs through `sqlcmd` and EF's `ExecuteSqlRaw`.
- **Acceptance:** the new test passes and the full suite is green.

## Task 2 — Linux publish + migration bundle
- `dotnet publish` the API, Web and Worker (`-r linux-x64 --self-contained`) into `publish/`, and build `publish/efbundle`.
- **Acceptance:** the four outputs exist and the ELF executables are present. `publish/` stays git-ignored, so there's nothing to commit.

## Task 3 — Checklist update
- Edit `docs/reports/2026-11-20-0257-demo-readiness/HOW-TO-USE.md`:
  - add the demo-data step after the migration bundle (`sqlcmd` inside the SQL container);
  - alerts go to the parent's **Contact email**: set Marites and Rodel to a real inbox (the same address is allowed);
  - demo script: Dennis skips today's session and flips to At risk live, while Aira scans.
- **Acceptance:** the steps match the script and the code.

## Task 4 — SSH key (needs the user)
- `ssh-keygen -t ed25519 -f ~/.ssh/klasly_vps`, with no passphrase so Claude can use it.
- The user runs one command to add the public key to the VPS, and gives the login user and the domain.
- **Acceptance:** `ssh -i ~/.ssh/klasly_vps <user>@<host> true` succeeds.

## Task 5 — VPS deploy (each step confirmed)
1. Docker, SQL Server container
2. copy `publish/` + `deploy/`
3. `efbundle`
4. `demo-data.sql`
5. env file: Claude generates the JWT/QR keys on the server; the user pastes the Brevo key and the Firebase JSON
6. systemd ×3
7. nginx + certbot
8. smoke test
- **Acceptance:** the spec's deploy criteria are met.

## After
- CLAUDE.md §5 report and an INDEX line.
- The APK is left to the teammate (see the spec's Out of scope).
