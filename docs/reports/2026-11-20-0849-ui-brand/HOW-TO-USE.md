# How to see the new Klasly logo and font

## Prerequisites
Nothing has to run to see the files. The website starts using them in the next milestone (M2).

## Steps
1. Open `src/Klasly.Web/wwwroot/img/klasly-logo.svg` in a browser: the teal K with a check for its lower leg, then "Klasly" in navy.
2. `klasly-mark.svg` is the symbol alone (favicon, app icon). `klasly-logo-reverse.svg` is the white version for dark backgrounds.
3. Android: a teammate with Android Studio runs `./gradlew assembleDebug` in `android/KlaslyApp` and installs the APK. The launcher icon is now a white K + check on teal.

## Example
On a phone's home screen the Klasly icon is a teal circle or rounded square with the white mark centred.

## Troubleshooting
- **The wordmark looks like Segoe UI, not Inter** when opening the SVG file directly: expected; the file falls back to the system font. Inside the website it uses the bundled Inter.
- **Android still shows the green robot**: uninstall the old app first; some launchers cache icons.
- **Gradle error about the vector**: check `strokeLineCap`/`strokeLineJoin` are spelled exactly as in the file (API 21+ supports them; minSdk is 26).
