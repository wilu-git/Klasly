# How to use — Milestone 13 Task 9 (checking the app on a phone or emulator)

The app's new screens are built and tested on the computer, but nobody has looked at them on a device yet. This is how a teammate can do that check.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database.
- The Web API running: `dotnet run --project src/Klasly.API --launch-profile https` (listens on `http://localhost:5201` for the emulator).
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
