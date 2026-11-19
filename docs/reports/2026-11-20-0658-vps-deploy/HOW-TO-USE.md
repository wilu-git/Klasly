# Using and managing Klasly on the VPS

## Prerequisites
- Nothing to start: SQL Server, the API, the Web site and the Worker all run on the VPS and come back after a reboot.
- To manage it: this PC's key `~/.ssh/klasly_vps` (login user `klasly`).

## Step by step
1. Open **https://klasly.site** and log in with a seeded account. The password is in the M3 HOW-TO-USE.
2. To check what's running:
   ```bash
   ssh -i ~/.ssh/klasly_vps klasly@167.104.103.188 "XDG_RUNTIME_DIR=/run/user/\$(id -u) systemctl --user status klasly-api klasly-web klasly-worker --no-pager"
   ```
3. To add the Brevo key:
   1. `ssh -i ~/.ssh/klasly_vps klasly@167.104.103.188`
   2. `nano ~/klasly/klasly.env` and fill in `Brevo__ApiKey=` and `Brevo__SenderEmail=`.
   3. `XDG_RUNTIME_DIR=/run/user/$(id -u) systemctl --user restart klasly-worker`
4. To add Firebase: copy the service-account JSON to `/home/klasly/klasly/fcm.json` (`chmod 600`), then restart `klasly-api` the same way.
5. Before the demo, as admin at https://klasly.site, go to Accounts › Parent › Edit **Marites** and **Rodel** and set their **Contact email** to a real inbox.
6. The APK: `klasly.apiBaseUrl=https://klasly.site/api/` in `android/KlaslyApp/local.properties`, then build a release APK. The rest is in the demo checklist (Part D).

## Example
Marites › My children (right now):
- **Aira**: ELECT3 On track (0% absent, 89.50), IT302 On track.
- **Bryan**: ELECT3 **At risk** (33% absent), IT302 On track (12.5%, 76.80).

## Troubleshooting
- **klasly.site shows a GoDaddy page on your PC:** that's an old DNS cache. Run `ipconfig /flushdns`, or test on a phone with mobile data.
- **A page says the API is unreachable:** run `journalctl --user -u klasly-api -n 50` as `klasly`. The service restarts by itself after a crash.
- **Logs show "Brevo is not configured":** the key or sender is still empty in `klasly.env`, or the worker wasn't restarted after the edit.
