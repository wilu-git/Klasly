# How to use — Milestone 13 device check (parent and student screens on the emulator)

This is how the new app screens were checked on the emulator, so you can repeat it on your PC or phone.

## Prerequisites
- SQL Server LocalDB with the `Klasly` database.
- The Web API started with the **http** profile:
  ```
  dotnet run --project src/Klasly.API --launch-profile http
  ```
  (With `--launch-profile https`, the app shows "Can't reach Klasly", because the API redirects it to an HTTPS address the emulator can't open.)
- Android Studio → Device Manager → start **Pixel_7** (with its window).

## Step by step
1. In `android/KlaslyApp`: `$env:JAVA_HOME = "D:\Android Studio\jbr"; ./gradlew installDebug`.
2. **Parent:** log in as `marites.mendoza@klasly.test` / `Klasly@123`.
   - You see Aira's classes with **At risk** badges. Tap **Show attendance records** on a card.
   - Tap the **Bryan Lacsamana** chip, then pull down to refresh.
3. **Log out.** On the login screen, clear the email box first (it remembers the last email).
4. **Student:** log in as `carla.dimaano@klasly.test` / `Klasly@123`. Allow the camera, then tap **Attendance** and **Grades** at the bottom.
5. When finished, reset Carla's phone in the web portal (Admin › Accounts › Carla › Reset phone), so a real phone can register later.

## Example
See the screenshots in this folder:
- `parent-child-overview.png`: Aira's overview with the child chips and At risk badges;
- `student-attendance.png`: Carla's attendance, absences in red, no badge;
- `student-grades.png`: Carla's grades, "Not graded yet" for the Midterm, weighted average 100.

## Troubleshooting
- **"Can't reach Klasly. Check your connection."**: the API is on the https profile, or not running. Restart it with `--launch-profile http`.
- **"Another device is already registered"**: that student already has a phone. Use another test student, or reset their phone in Admin › Accounts.
- **The email box shows two emails joined together**: clear the box before typing; it keeps the last email after logout.
