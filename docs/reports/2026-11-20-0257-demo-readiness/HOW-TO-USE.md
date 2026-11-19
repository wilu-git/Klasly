# Demo checklist: Klasly on the VPS + APK

Work through it top to bottom. Tick each box as you go. The test password for every seeded account is in `docs/reports/2026-11-09-1906-milestone-3-auth/HOW-TO-USE.md`.

## Prerequisites
- An Ubuntu VPS (≥ 2 GB RAM for SQL Server), SSH access, and a domain whose A record points to the VPS.
- A laptop with the .NET 10 SDK and `dotnet-ef` 10.x (this repo).
- A machine with Android Studio, for the APK.
- A Firebase account, a free Brevo account, and an Android phone.

## Part B — Deploy to the VPS
1. [ ] **DNS:** the A record `<domain>` points to the VPS IP. Check with `ping <domain>`.
2. [ ] **SQL Server** (Docker, local only):
   ```bash
   sudo docker run -d --name klasly-sql --restart always -e ACCEPT_EULA=Y -e MSSQL_PID=Express -e 'MSSQL_SA_PASSWORD=<strong password>' -p 127.0.0.1:1433:1433 -v klasly-sql:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
   ```
   Don't put quotes or backslashes in the password (the systemd env file treats them specially).
3. [ ] **Publish** on the laptop, from the repo root:
   ```bash
   dotnet publish src/Klasly.API -c Release -r linux-x64 --self-contained -o publish/api
   ```
   ```bash
   dotnet publish src/Klasly.Web -c Release -r linux-x64 --self-contained -o publish/web
   ```
   ```bash
   dotnet publish src/Klasly.Worker -c Release -r linux-x64 --self-contained -o publish/worker
   ```
   ```bash
   dotnet ef migrations bundle -p src/Klasly.Infrastructure -s src/Klasly.API -r linux-x64 --self-contained -o publish/efbundle
   ```
4. [ ] **Copy** to the VPS:
   - `scp -r publish deploy <user>@<domain>:~/`
   - then on the VPS:
   ```bash
   sudo useradd --create-home klasly && sudo mkdir -p /opt/klasly /etc/klasly && sudo cp -r ~/publish/api ~/publish/web ~/publish/worker /opt/klasly/ && sudo chmod +x /opt/klasly/*/Klasly.* && sudo chown -R klasly /opt/klasly
   ```
5. [ ] **Create the database and seed it:**
   ```bash
   chmod +x ~/publish/efbundle && ~/publish/efbundle --connection "Server=127.0.0.1,1433;Database=Klasly;User Id=sa;Password=<sa password>;TrustServerCertificate=True"
   ```
6. [ ] **Secrets:**
   - `sudo cp ~/deploy/klasly.env.example /etc/klasly/klasly.env` and fill it in (`sudo nano`).
   - Make the two keys with `openssl rand -base64 48`.
   - Then `sudo chown klasly /etc/klasly/klasly.env && sudo chmod 600 /etc/klasly/klasly.env`.
7. [ ] **Services:**
   ```bash
   sudo cp ~/deploy/klasly-*.service /etc/systemd/system/ && sudo systemctl daemon-reload && sudo systemctl enable --now klasly-api klasly-web klasly-worker
   ```
8. [ ] **nginx + HTTPS:**
   - `sudo apt install nginx certbot python3-certbot-nginx`
   - copy `deploy/nginx-klasly.conf` to `/etc/nginx/sites-available/klasly` and replace `example.com`;
   - link it into `sites-enabled`, then `sudo nginx -t && sudo systemctl reload nginx`;
   - `sudo certbot --nginx -d <domain>`.
9. [ ] **Smoke test:**
   - `curl -i https://<domain>/api/auth/me` → `401`;
   - `https://<domain>/` in a browser → the Klasly login page;
   - log in as `admin@klasly.test`.

## Part C — Firebase and Brevo
1. [ ] **Firebase:**
   - Create a project, then Add app › Android, package **`com.klasly.app`**.
   - Download `google-services.json` into `android/KlaslyApp/app/` (it's git-ignored).
2. [ ] **Firebase service account:**
   - Project settings › Service accounts › Generate new private key.
   - `scp` it to the VPS, then `sudo mv` it to `/etc/klasly/fcm.json`, `chown klasly`, `chmod 600`.
   - `Fcm__ServiceAccountPath=/etc/klasly/fcm.json` is already in the env file.
3. [ ] **Brevo:**
   - Senders › add and verify your sender email.
   - SMTP & API › create an API key.
   - Put both in the env file (`Brevo__ApiKey`, `Brevo__SenderEmail`).
4. [ ] `sudo systemctl restart klasly-api klasly-worker`.
5. [ ] **Real inbox for alerts:** as admin, Accounts › Parent › Edit **Marites Mendoza** and set her email to an inbox you can open on screen. Seeded `@klasly.test` addresses can't receive mail.

## Part D — APK
1. [ ] In `android/KlaslyApp/local.properties`, add `klasly.apiBaseUrl=https://<domain>/api/`.
2. [ ] `google-services.json` is in `android/KlaslyApp/app/`.
3. [ ] Android Studio › **Build › Generate Signed App Bundle / APK › APK**:
   - create a keystore and keep it outside the repo (`*.jks` is git-ignored anyway);
   - choose **release**.
4. [ ] Copy the APK to the phone and install it (allow "unknown sources").
5. [ ] Log in as **Aira Mendoza**. The phone is now registered to her.

## Part E — Demo-day settings
- [ ] `Worker__Cron=* * * * *` (every minute) is in the env file, and the worker has been restarted since.
- [ ] One phone per student. "Another device is already registered" → **Admin › Accounts › Student › Reset phone**.
- [ ] Fallback (VPS down): run locally with `dotnet run --project src/Klasly.API --launch-profile lan` + the MVC `https` profile + the Worker. Point the APK at `http://<laptop IP>:5201/api/` (debug build). See the M14 HOW-TO-USE for the firewall rule.

## Part F — Demo script (rehearse once tonight)
1. **Admin controls** (admin, browser):
   - Terms: the active term.
   - Subjects: create/edit.
   - Accounts:
     - role filter, New student;
     - Parent › Edit › link/unlink a child;
     - Deactivate (a logged-in user is logged out at once), then Edit › tick Active;
     - Reset phone.
   - Classes: enroll/drop.
   - Failed emails.
2. **Attendance** (Rosa Villanueva → ELECT3 class):
   - Start a session about 10 min long (late after 15 min). The QR changes every 30 s.
   - Aira scans → "Checked in · Present" → her name appears in the live list within 5 s. Scanning again → "already checked in".
   - **Close** the session. Within about 1 min the other students turn **Absent** and the session is Finalized.
   - Correction: set Dennis to **Excused** (Excused doesn't count in the absence rate).
3. **Grades** (Rosa):
   - New assessment "Quiz 1", 20 pts, 30%. Over 100% total weight is refused.
   - Scores: 21/20 is refused. Aira 18, Carla 10.
   - Aira's phone gets a "grade posted" push.
4. **At-risk indicators** (after the next tick, about 1 min):
   - Rule: absence rate ≥ 20% **or** weighted average < 75.
   - Marites (web) › My children: **Aira "On track"**, **Bryan "At risk"** (absent).
   - Rodel (web): **Carla "At risk"** (grade 50).
   - Phone: log out Aira, log in Marites › Child overview shows the same badges.
   - Aira's own Attendance/Grades tabs never show the at-risk flag (on purpose).
5. **Alerts:**
   - Marites's real inbox gets the absence and at-risk emails.
   - Anything that failed shows on Admin › Failed emails.

## Example
- Accounts › Student: each row has **Edit · Reset phone · Deactivate**.
- Reset phone asks "Reset Aira Mendoza's phone?…", then shows "Phone reset. The student's next login on the app registers their new phone."

## Troubleshooting
- **The MVC site shows "the API is unreachable":**
  - `sudo systemctl status klasly-api` and `journalctl -u klasly-api -n 50`. A missing or short `Jwt__Key` / `Qr__SigningKey` stops the API at startup.
  - Check that `Api__BaseUrl=http://127.0.0.1:5000/`.
- **Logged out after every restart of klasly-web:** user `klasly` has no home dir, so login keys can't be saved. Fix it with `sudo mkhomedir_helper klasly`, or recreate the user with `--create-home`.
- **Emails stay Failed "Brevo is not configured":** the env file is missing `Brevo__ApiKey` / `Brevo__SenderEmail`, or the worker wasn't restarted. Rows that already failed aren't re-sent; new alerts are.
- **The phone app says it can't connect:** open `https://<domain>/api/auth/me` in the phone's browser. If that works, the APK was built without `klasly.apiBaseUrl`, so rebuild it.
