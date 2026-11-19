# VPS deploy: Klasly live at https://klasly.site (Task 5 of `tasks/plan-demo.md`)

## Summary
Asked: deploy Klasly with demo data. Done:
- the API, MVC site and Worker run on the shared Ubuntu 22.04 VPS (167.104.103.188), next to the existing `bemyparol.site` and dalan sites;
- `klasly.site` was moved to Cloudflare DNS (grey cloud), and Caddy issued the HTTPS certificate;
- the database was created by the migration bundle, and the four-week demo history was loaded;
- the Worker's first tick flagged Bryan and Carla at risk, as the spec planned.

## Projects touched
WEB API, WEB MVC, CONSOLE WORKER, MSSQL (deploy only). No code change this step.

## Files changed
| Path | Change | Why |
|---|---|---|
| `deploy/klasly-*.service` | modified (`b00fbb4`) | systemd **user** services of `klasly`, so they never touch the other sites' system services |
| `deploy/klasly.caddy` | added (`b00fbb4`) | the Klasly site for the server's existing Caddy |
| `deploy/nginx-klasly.conf` | deleted (`b00fbb4`) | Caddy already owns ports 80/443 |
| `deploy/klasly.env.example` | modified (`b00fbb4`) | SQL on `127.0.0.1:14330`; the FCM path is in the klasly home |
| `docs/reports/2026-11-20-0257-demo-readiness/HOW-TO-USE.md` | modified (`4e110b6`) | the migration bundle needs the settings as environment variables |
| `SPEC.md` | modified | deploy criterion ticked |

## Server layout
- `/home/klasly/klasly/`:
  - `api`, `web`, `worker`, `efbundle`, `deploy`;
  - `klasly.env` and `sql.env` (`chmod 600`; the JWT, QR and SA values were generated on the server and never printed);
  - `root-steps.sh` (the demo-data load and the Caddy site, run once with sudo).
- SQL Server 2022 Express: container `klasly-sql` on `127.0.0.1:14330`, volume `klasly-sql`. Port 1433 belongs to the other site.
- Services: `systemctl --user` as `klasly` (`enable-linger` on). The API listens on `127.0.0.1:5000`, the Web on `127.0.0.1:5001`.
- Caddy: `/etc/caddy/klasly.caddy`, imported from the Caddyfile. The backup is `/etc/caddy/Caddyfile.pre-klasly`.

## Database changes
None to the schema. The `InitialCreate` migration applied the schema and seed, then `demo-data.sql` added 20 sessions, 96 attendance records, 5 assessments and 24 grades. Hangfire created its own `HangFire` schema.

## How it was verified
- `https://klasly.site/api/auth/me` → 401 and `https://klasly.site/` → "Log in - Klasly", with a valid certificate (tested with `--resolve`, because this PC still had the old GoDaddy address cached).
- `bemyparol.site` → 200 after the Caddy reload.
- First Worker tick: "risk for 9 enrollments, 9 new snapshots, 2 at-risk", "queued 0 absence and 2 at-risk emails", and 2 marked Failed ("Brevo is not configured"), as expected.
- Parent API on the server: Bryan ELECT3 0.3333 / 79.5 **at risk**; Carla ELECT3 0.0833 / 65.75 **at risk**; Dennis 0.1667 / 79.25 on track; Aira on track. This matches `SPEC.md` exactly.

## Known limits / TODO
- **Brevo:** the key and sender aren't set, so the two initial at-risk emails are Failed for good. They show on Admin › Failed emails. New alerts send once the key is set and the worker is restarted.
- **Firebase:** `fcm.json` isn't on the server yet, and the APK isn't built (no JDK on this PC).
- **Parents' contact emails** are still `@klasly.test`. Change Marites and Rodel to a real inbox before the demo.
- **Security:**
  - the seeded test password is public in the repo, and the site is on the internet;
  - the deploy SSH key `~/.ssh/klasly_vps` has no passphrase.
  - After the demo, stop the services (`systemctl --user disable --now klasly-api klasly-web klasly-worker`) and remove the key from `/home/klasly/.ssh/authorized_keys`.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED` is set, but the API still logs "Failed to determine the https port for redirect". This is harmless: Caddy does the HTTPS, and there's no redirect loop.
