# Demo checklist: Klasly on the VPS + APK

Work through it top to bottom. Tick each box as you go. The test password for every seeded account is in `docs/reports/2026-11-09-1906-milestone-3-auth/HOW-TO-USE.md`.

## Prerequisites
- The shared Ubuntu VPS (≥ 2 GB RAM free for SQL Server) with Docker and **Caddy already running** for the other site, SSH access with `sudo`, and a domain whose A record points to the VPS.
- A laptop with the .NET 10 SDK and `dotnet-ef` 10.x (this repo).
- A machine with Android Studio, for the APK.
- A Firebase account, a free Brevo account, and an Android phone.

How the VPS is laid out: Klasly runs as its own `klasly` user. Everything lives in `/home/klasly/klasly/` (`api`, `web`, `worker`, `deploy`, `klasly.env`, `fcm.json`). The three services are systemd **user** services of `klasly`, so they never touch the other site's system services. SQL Server is Klasly's own container on `127.0.0.1:14330`, because 1433 belongs to the other site. Caddy routes `<domain>/api/*` to the API on port 5000 and everything else to the MVC site on port 5001.

## Part B — Deploy to the VPS
1. [ ] **DNS and free ports:**
   - The A record `<domain>` points to the VPS IP. Check with `ping <domain>`.
   - On the VPS, `ss -ltn | grep -E ':(5000|5001|14330)\b'` prints nothing. If it prints a line, another app already uses that port.
2. [ ] **SQL Server** (Klasly's own container, local only, port 14330):
   ```bash
   sudo docker run -d --name klasly-sql --restart always -e ACCEPT_EULA=Y -e MSSQL_PID=Express -e 'MSSQL_SA_PASSWORD=<strong password>' -p 127.0.0.1:14330:1433 -v klasly-sql:/var/opt/mssql mcr.microsoft.com/mssql/server:2022-latest
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
   - then on the VPS, create the `klasly` user, let its services run without anyone logged in (`enable-linger`), and move the files into its home:
   ```bash
   sudo useradd --create-home klasly; sudo loginctl enable-linger klasly && sudo mkdir -p /home/klasly/klasly && sudo cp -r ~/publish/api ~/publish/web ~/publish/worker ~/deploy /home/klasly/klasly/ && sudo chmod +x /home/klasly/klasly/*/Klasly.* && sudo chown -R klasly:klasly /home/klasly/klasly
   ```
   If `useradd` says the user already exists, that's fine; the rest still runs.
5. [ ] **Create the database and seed it:**
   ```bash
   chmod +x ~/publish/efbundle && cd /home/klasly/klasly && env ASPNETCORE_ENVIRONMENT=Production "$(grep ^ConnectionStrings__Klasly= klasly.env)" "$(grep ^Jwt__Key= klasly.env)" "$(grep ^Qr__SigningKey= klasly.env)" ~/publish/efbundle --connection "$(grep ^ConnectionStrings__Klasly= klasly.env | cut -d= -f2-)"
   ```
   Run this after step 7 (it reads `klasly.env`). The bundle starts the API's own startup code, which stops with "Connection string 'Klasly' is missing" unless the connection string and both keys are set as environment variables. `--connection` alone isn't enough. If SQL Server was started seconds ago, wait about 30 s and run it again.
   Then **load the four-week demo history** (run it once; a second run changes nothing):
   ```bash
   sudo docker exec -i klasly-sql /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P '<sa password>' -d Klasly -b < ~/deploy/demo-data.sql
   ```
   It should print "Demo data loaded: 20 finalized sessions…". The badges appear once the Worker runs (Part C, step 5).
6. [ ] **Switch to the `klasly` user** for steps 7–8. Every `systemctl --user` command in this checklist runs inside this shell:
   ```bash
   sudo -iu klasly
   ```
   ```bash
   export XDG_RUNTIME_DIR=/run/user/$(id -u)
   ```
   The second line lets `systemctl --user` find the user's service manager. Without it you get "Failed to connect to bus".
7. [ ] **Secrets** (as `klasly`):
   - `cp ~/klasly/deploy/klasly.env.example ~/klasly/klasly.env && chmod 600 ~/klasly/klasly.env`, then fill it in with `nano ~/klasly/klasly.env`.
   - Make the two keys with `openssl rand -base64 48`.
   - The connection string already uses port 14330, and `Worker__Cron` is already every minute.
8. [ ] **Services** (as `klasly`):
   ```bash
   mkdir -p ~/.config/systemd/user && cp ~/klasly/deploy/klasly-*.service ~/.config/systemd/user/ && systemctl --user daemon-reload && systemctl --user enable --now klasly-api klasly-web
   ```
   Check with `systemctl --user status klasly-api klasly-web`: both should say `active (running)`. Then `exit` back to your own user.
   Don't start the Worker yet. Its first run sends the at-risk emails for Bryan and Carla, so Brevo and the parents' contact emails must be ready first (Part C).
9. [ ] **Caddy + HTTPS** (as your own user, with `sudo`). This adds a Klasly site next to the existing one and backs up the Caddyfile first:
   ```bash
   sed -i 's/klasly.example.com/<domain>/' ~/deploy/klasly.caddy
   ```
   ```bash
   sudo cp /etc/caddy/Caddyfile /etc/caddy/Caddyfile.pre-klasly && sudo cp ~/deploy/klasly.caddy /etc/caddy/klasly.caddy && echo 'import /etc/caddy/klasly.caddy' | sudo tee -a /etc/caddy/Caddyfile
   ```
   ```bash
   sudo caddy validate --config /etc/caddy/Caddyfile && sudo systemctl reload caddy
   ```
   - Run the `import` line only once. A second run adds a duplicate and `caddy validate` fails.
   - Caddy gets the HTTPS certificate on its own once DNS points here. No certbot.
   - If the other site breaks, put the backup back: `sudo cp /etc/caddy/Caddyfile.pre-klasly /etc/caddy/Caddyfile && sudo systemctl reload caddy`.
10. [ ] **Smoke test:**
   - `curl -i https://<domain>/api/auth/me` → `401`;
   - `https://<domain>/` in a browser → the Klasly login page;
   - log in as `admin@klasly.test`.

## Part C — Firebase and Brevo
1. [ ] **Firebase:**
   - Create a project, then Add app › Android, package **`com.klasly.app`**.
   - Download `google-services.json` into `android/KlaslyApp/app/` (it's git-ignored).
2. [ ] **Firebase service account:**
   - Project settings › Service accounts › Generate new private key.
   - `scp` it to the VPS as `fcm.json`, then:
   ```bash
   sudo mv ~/fcm.json /home/klasly/klasly/fcm.json && sudo chown klasly:klasly /home/klasly/klasly/fcm.json && sudo chmod 600 /home/klasly/klasly/fcm.json
   ```
   - `Fcm__ServiceAccountPath=/home/klasly/klasly/fcm.json` is already in the env file.
3. [ ] **Brevo:**
   - Senders › add and verify your sender email.
   - SMTP & API › create an API key.
   - Put both in the env file (`Brevo__ApiKey`, `Brevo__SenderEmail`) with `sudo -u klasly nano /home/klasly/klasly/klasly.env`.
4. [ ] **Real inbox for alerts:**
   - As admin, go to Accounts › Parent › Edit **Marites Mendoza** and **Rodel Dimaano**. Set their **Contact email**, not the login email, to an inbox you can open on screen. Alerts go to the contact email.
   - Both can use the same address. With Gmail, `you+marites@gmail.com` / `you+rodel@gmail.com` tells them apart.
   - Seeded `@klasly.test` addresses can't receive mail.
5. [ ] Restart the API, then start the Worker. As `klasly` (Part B, step 6: `sudo -iu klasly`, then the `export` line):
   ```bash
   systemctl --user restart klasly-api && systemctl --user enable --now klasly-worker
   ```
   Within a minute, Marites's child page shows Bryan "At risk", and Rodel's inbox gets Carla's at-risk email.

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
   - The class page already lists four weeks of finished sessions (demo history). Open one to show its roster.
   - **Close** today's session. Within about 1 min the other students turn **Absent** and the session is Finalized.
   - Correction: set Francis to **Excused** (Excused doesn't count in the absence rate). **Leave Dennis Absent.**
3. **Grades** (Rosa):
   - The class already has Quiz 1, Laboratory 1 and Midterm Exam (60% of the weight).
   - New assessment "Quiz 2", 20 pts: try 50% → refused (only 40% is left), then 20%.
   - Scores: 21/20 is refused. Aira 18, Carla 10.
   - Aira's phone gets a "grade posted" push.
4. **At-risk indicators:**
   - Rule: absence rate ≥ 20% **or** weighted average < 75 (the SystemSettings thresholds).
   - **Before the session** (from the demo history):
     - Marites (web) › My children: **Aira "On track"**, **Bryan "At risk"** (4 of 12 absent, 33%).
     - Bryan's IT302 card shows "On track" (1 of 8), so the flag is per class.
     - Rodel: **Carla "At risk"** (average 65.75), **Dennis "On track"** (2 of 12, 17%).
   - **After closing today's session** (next tick, about 1 min): Dennis's absence makes 3 of 13 = 23%, so Rodel's page now shows **Dennis "At risk"**. This is the live moment.
   - Phone: log out Aira, log in Marites › Child overview shows the same badges.
   - Aira's own Attendance/Grades tabs never show the at-risk flag (on purpose).
5. **Alerts:**
   - Rodel's inbox gets today's absence emails for Carla and Dennis, then Dennis's at-risk email. Marites's gets Bryan's absence email.
   - Anything that failed shows on Admin › Failed emails.

## Example
- Accounts › Student: each row has **Edit · Reset phone · Deactivate**.
- Reset phone asks "Reset Aira Mendoza's phone?…", then shows "Phone reset. The student's next login on the app registers their new phone."

## Troubleshooting
- **The MVC site shows "the API is unreachable":**
  - As `klasly`: `systemctl --user status klasly-api`. From your own user: `sudo journalctl _SYSTEMD_USER_UNIT=klasly-api.service -n 50`. A missing or short `Jwt__Key` / `Qr__SigningKey` stops the API at startup, and so does a wrong SQL port (it must be 14330).
  - Check that `Api__BaseUrl=http://127.0.0.1:5000/`.
- **`systemctl --user` says "Failed to connect to bus":** you skipped the `export XDG_RUNTIME_DIR=/run/user/$(id -u)` line after `sudo -iu klasly`. If it still fails, check `loginctl show-user klasly | grep Linger` says `Linger=yes`; if not, run `sudo loginctl enable-linger klasly`.
- **The services stop when you log out of SSH:** linger is off. Same fix: `sudo loginctl enable-linger klasly`.
- **The domain shows the other site, or no certificate:** check `/etc/caddy/Caddyfile` ends with `import /etc/caddy/klasly.caddy`, and that `klasly.caddy` has your domain, not `klasly.example.com`. Then `sudo systemctl reload caddy` and `sudo journalctl -u caddy -n 50`.
- **Emails stay Failed "Brevo is not configured":** the env file is missing `Brevo__ApiKey` / `Brevo__SenderEmail`, or the worker wasn't restarted. Rows that already failed aren't re-sent; new alerts are.
- **The phone app says it can't connect:** open `https://<domain>/api/auth/me` in the phone's browser. If that works, the APK was built without `klasly.apiBaseUrl`, so rebuild it.
