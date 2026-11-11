# How to use — Milestone 13 Task 9 (checking the app on a phone or emulator)

This is how to check the app's new screens on a phone or emulator. The check passed on the Pixel_7 emulator on a second try (see `docs/reports/2026-11-11-1150-milestone-13-device-check/`); repeat it on a real phone before the demo.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database.
- The Web API running with the **http** profile: `dotnet run --project src/Klasly.API --launch-profile http` (`http://localhost:5201`). The `https` profile redirects the app to `https://localhost:7256`, which the emulator can't reach.
- Android Studio with an emulator opened **in its window** (Device Manager → ▶), or a phone set up as in Milestone 8.

## Step by step
1. In `android/KlaslyApp`: `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew installDebug`.
2. Open Klasly and log in as student `aira.mendoza@klasly.test` / `Klasly@123`. If the app says another phone is registered, use `carla.dimaano@klasly.test`.
3. Tap **Attendance**, then **Grades**. The numbers should match the parent web page for the same student. Pull down to refresh.
4. Log out, then log in as parent `marites.mendoza@klasly.test`. Tap the **Bryan Lacsamana** chip, then **Aira Mendoza**. Tap **Show grades** on a card.
5. Optional: turn on TalkBack and check it reads "At risk" / "On track".

## Example
Parent screen, top of the list:
> **Aira Mendoza** · 2026-00001 · 1st Sem 2026-27 — [Aira Mendoza] [Bryan Lacsamana]
> **CPE 401 · Embedded Systems Design** — **At risk** · Absence rate 100%

## Troubleshooting
- **"Can't reach Klasly. Check your connection."**: the API isn't running, or the emulator's network isn't up yet. Wait until the emulator shows Wi-Fi, then tap **Try again**.
- **"Another device is already registered"** for a student: log in as another test student, or ask the admin to reset the device (Admin › Accounts › Reset phone).
- **The emulator is very slow or shows "System UI isn't responding"**: close it, then start it again from Android Studio with "Cold boot" and wait until the home screen settles.
