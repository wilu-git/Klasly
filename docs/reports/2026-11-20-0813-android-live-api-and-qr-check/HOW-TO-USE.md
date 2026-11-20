# How to point the Android app at the live site and test a scan

## Prerequisites
- Android Studio with the SDK installed. Gradle needs `JAVA_HOME` set to Android Studio's `jbr` folder when run from a terminal.
- `https://klasly.site` up: the API, the MVC portal and the Worker on the VPS.
- A phone or emulator with a camera.

## Steps
1. In `android/KlaslyApp/local.properties` add `klasly.apiBaseUrl=https://klasly.site/api/`. The trailing `/` is now optional.
2. Sync Gradle, then build: **Build › Build APK(s)**, or `./gradlew assembleDebug`.
3. Install `app/build/outputs/apk/debug/app-debug.apk` on the phone.
4. On a laptop, log in to `https://klasly.site` as `rosa.villanueva@klasly.test`. Open an ELECT3 session; the QR now rotates every 30 s.
5. On the phone, log in as a student enrolled in ELECT3 (see `docs/TEST-ACCOUNTS.md`), open **Scan** and point the camera at the QR.
6. The phone shows "Present" (or "Late" after the late cut-off), and the student appears in the teacher's live list.

## Example
The scan request the app sends, and the API's answer:
```
POST https://klasly.site/api/attendance/scan
{ "qrToken": "<from the QR>", "deviceId": "<phone id>" }
→ 200 { "status": "Present", "scannedAtUtc": "...", "subjectCode": "ELECT3", "section": "BSCpE 3-A" }
```

## Troubleshooting
- **The app closes the moment it starts:** you built the APK before this fix and the URL has no `/` at the end. Rebuild.
- **"Another device is already registered":** the student is bound to another phone. As admin, use **Accounts › Reset phone**.
- **"QR code expired":** the phone took more than one 30-second step to scan. Scan the new QR.
