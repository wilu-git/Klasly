# How to use the Klasly app: log in and scan your teacher's QR

## Prerequisites
- **SQL Server LocalDB** with the `Klasly` database (Milestone 2).
- **The Web API** on the `http` profile (the app can't use the https one):
  ```powershell
  dotnet run --project src/Klasly.API --launch-profile http
  ```
- **The website**, for the teacher's QR: `dotnet run --project src/Klasly.Web --launch-profile https`.
- **The app** on the `Pixel_9` emulator, or on an Android phone (8.0 or newer) connected by USB with USB debugging on.
- Seed accounts use the test password from the Milestone 2/5 how-tos. All students are in ELECT3 (teacher `rosa.villanueva@klasly.test`).

## Step by step

### Install the app
1. In PowerShell, inside `android\KlaslyApp`:
   ```powershell
   $env:JAVA_HOME = "D:\Android Studio\jbr"
   .\gradlew.bat installDebug
   ```
2. **Real phone only:** run `adb reverse tcp:5201 tcp:5201`, add the line `klasly.apiBaseUrl=http://localhost:5201/api/` to `android\KlaslyApp\local.properties`, then run `installDebug` again. (The emulator doesn't need this.)

### Log in
3. Open **Klasly**, type your school email and password, and tap **Log in** (or press Done on the keyboard).
   - **Student:** the first phone you log in on becomes *your* phone, and the Scan screen opens. The app remembers you until your login expires.
   - **Parent:** you see "Hi, {your name}". Your child overview comes in a later update.
   - **Teacher or admin:** use the website instead.

### Check in
4. The teacher starts a session on the website, and a QR code appears on the screen.
5. On the Scan screen, allow the camera the first time, then point the phone at the QR.
6. Within a second or two you see **Present** (green) or **Late** (amber), the class (e.g. "ELECT3 · BSCpE 3-A") and the time. Tap **Done**.
7. If you aren't checked in, the card says **Not checked in** and why. Tap **Scan again**.

## Example: what the screens say

| Situation | The app shows |
|---|---|
| Wrong password | "Invalid email or password." |
| API not running / no network | "Can't reach Klasly. Check your connection." |
| Logged in on a second phone | "Another device is already registered. Ask the admin to reset it." |
| Scanned an old photo of the QR | Not checked in · "QR expired – rescan" |
| Scanned twice | Not checked in · "Already checked in" |
| Session ended | Not checked in · "Session is closed" |
| Not in that class | Not checked in · "Not enrolled in this class" |
| Login expired | Back to Login with "Session expired – please log in" |

## Troubleshooting
- **"Can't reach Klasly" on the emulator:** start the API with `--launch-profile http`. With the https profile, the API redirects the phone to `https://localhost:7256`, which the emulator can't reach.
- **"Can't reach Klasly" on a real phone:** run `adb reverse tcp:5201 tcp:5201` again (it resets when you unplug the phone). Also check that `local.properties` has `klasly.apiBaseUrl=http://localhost:5201/api/` and that you reinstalled afterwards.
- **"Another device is already registered":** an admin resets the phone with `DELETE /api/accounts/{userId}/device` in Swagger (there's no button yet). This also happens if the app was installed from a different PC, because each PC's debug build gives the phone a different ID.
- **The first login after starting the API fails with an error page:** LocalDB was still waking up. Wait a few seconds and log in again.
