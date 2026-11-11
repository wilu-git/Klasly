# How to use — Milestone 14 (security tests, phone over Wi-Fi, Failed emails)

## Prerequisites
- SQL Server LocalDB running (`sqllocaldb start MSSQLLocalDB`).
- For the phone: the PC and the phone on the **same network**. The safest choice is the PC joined to the **phone's hotspot**, because campus Wi-Fi often blocks phone-to-PC traffic.
- For Failed emails: the API and the website running.

## Run the security and boundary tests
1. From the solution folder run:
   ```bash
   dotnet test Klasly.slnx
   ```
2. You should see `total: 154, failed: 0`. The new test classes are:
   - `RoleAccessTests`: every endpoint against every role;
   - `OwnershipTests`: other people's classes and children;
   - `ScanTests`: QR scan rules;
   - the exact-boundary test in `RiskCalculatorTests`.

## Use the app on a real phone without a cable
1. **Once, in PowerShell opened as Administrator** (right-click PowerShell → *Run as administrator*):
   ```powershell
   New-NetFirewallRule -DisplayName "Klasly API 5201" -Direction Inbound -Protocol TCP -LocalPort 5201 -Action Allow -Profile Private
   ```
   Also make sure Windows calls this network **Private**: Settings → Network & Internet → (your Wi-Fi) → Network profile type → Private.
2. Find the PC's address: run `ipconfig` and take the **IPv4 Address** of the Wi-Fi adapter, for example `192.168.0.114`. Ignore `192.168.56.x`; that is VirtualBox.
3. Start the API so the network can reach it:
   ```bash
   dotnet run --project src/Klasly.API --launch-profile lan
   ```
4. On the phone's browser, open `http://<PC address>:5201/api/auth/me`. A short JSON message that says "Session expired – please log in" means the network path works.
5. In `android\KlaslyApp\local.properties` set `klasly.apiBaseUrl=http://<PC address>:5201/api/`. Then, from `android\KlaslyApp`, run:
   ```bash
   ./gradlew installDebug
   ```
6. Unplug the cable. Log in as a student, for example `carla.dimaano@klasly.test` / `Klasly@123`, and scan the QR from the teacher's session page on the PC.

## Failed emails (admin)
1. Start the API and the website. Log in at `https://localhost:7144` as `admin@klasly.test` / `Klasly@123`.
2. Click **Failed emails** in the top bar.
3. **Example:** the screen shows a table, newest first, with these columns:
   - **Created:** Manila time;
   - **Recipient:** the parent's name and email;
   - **Alert:** Absence or At risk, plus the subject line;
   - **Attempts**;
   - **Last error:** for example "Brevo is not configured".

   With no failures it says "No failed emails. Every alert reached Brevo." See `admin-failed-emails.jpg`.
4. API version:
   ```bash
   curl -H "Authorization: Bearer <admin token>" https://localhost:7256/api/notifications/failed
   ```
   This returns
   ```json
   [{ "notificationId": 114, "createdAtUtc": "2026-11-11T00:49:00", "recipientName": "Marites Mendoza",
      "recipientEmail": "marites.mendoza@klasly.test", "type": "AtRisk", "subject": "Klasly: …", "attempts": 0,
      "lastError": "Brevo is not configured" }]
   ```

## Troubleshooting
- **The first `dotnet test` run fails with database connection errors:** LocalDB was still starting. Run it again.
- **The phone's browser can't open the address:** check these in order:
  - the firewall rule (step 1);
  - the network is Private;
  - both devices are on the same Wi-Fi or hotspot;
  - the API was started with `--launch-profile lan`, not `http`.

  On campus Wi-Fi, switch to the phone's hotspot.
- **"Another device is already registered" on the phone:** in the website go to Admin › Accounts › (student) › Reset phone, then log in again.
