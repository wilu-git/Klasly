# How to open and build the Klasly Android app (shell)

This is only the empty starting app. It shows "Hello Android!". Login and QR scanning come in later tasks of Milestone 8.

## Prerequisites
- Android Studio 2026.1 or newer (it includes the Java 21 that Gradle needs).
- The Android SDK (Android Studio installs it). On the team PC it is at `D:\Android\sdk`.
- No SQL Server, API, Worker or website is needed for this step.

## Step by step
1. In Android Studio choose **File → Open** and pick the folder `Klasly\android\KlaslyApp` (not the repo root).
2. If Android Studio asks, click **Trust Project**. It creates `local.properties` with your SDK path. Never commit that file (it's already ignored).
3. Wait for **Gradle sync** to finish (the first time, it downloads libraries and can take several minutes).
4. Pick the `Pixel_9` emulator in the device menu and press **Run ▶**.
5. From the command line instead (PowerShell, inside `android\KlaslyApp`):
   ```powershell
   $env:JAVA_HOME = "D:\Android Studio\jbr"
   .\gradlew.bat assembleDebug
   ```
   The APK is at `app\build\outputs\apk\debug\app-debug.apk`.

## Example
The emulator opens an app called **KlaslyApp** with a white screen and the text "Hello Android!" at the top left. That's expected for now.

## Troubleshooting
- **`JAVA_HOME is not set` / `java not found`** when running `gradlew` from a terminal: run `$env:JAVA_HOME = "D:\Android Studio\jbr"` first (Android Studio's own Java).
- **`SDK location not found`**: open the project once in Android Studio, or create `local.properties` with `sdk.dir=D\:\\Android\\sdk` (your own SDK path).
- **`./gradlew: Permission denied` or `bad interpreter` on macOS/Linux**: run `chmod +x gradlew`. `.gitattributes` keeps the file's line endings correct.
