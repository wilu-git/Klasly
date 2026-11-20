# UI redesign M1 — `ui-brand`: logo, favicon, Inter, Android icon

## Summary
Milestone M1 of the Web UI/UX redesign (SPEC.md §A, plan `tasks/ui-redesign-plan.md`). The team's uploaded concept (`docs/brand/klasly-logo-concept.png`, `klasly-app-icon-concept.png`) is the approved mark: a teal K whose lower leg is a check, with rounded caps. It was redrawn as vector SVG on a 24-unit grid, plus the horizontal and reverse logos and an SVG favicon. Inter v4.1 (variable, upright) is bundled locally with its licence. The Android adaptive icon uses the same mark in white on teal. Nothing references the new web assets yet; `_Layout` picks them up in M2.

## Projects touched
WEB MVC (static assets only), ANDROID KOTLIN (launcher drawables only), docs.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/wwwroot/img/klasly-mark.svg` | added | Symbol only, teal `#126B64`, 24-unit viewBox; source of every other variant |
| `src/Klasly.Web/wwwroot/img/klasly-logo.svg` | added | Symbol + "Klasly" wordmark (`<text>`, Inter/system stack, 600, navy `#17324D`) |
| `src/Klasly.Web/wwwroot/img/klasly-logo-reverse.svg` | added | White symbol + wordmark for navy/teal backgrounds |
| `src/Klasly.Web/wwwroot/favicon.svg` | added | Same as the symbol; linked from `_Layout` in M2 |
| `src/Klasly.Web/wwwroot/fonts/InterVariable.woff2` | added | Inter v4.1 upright variable font (352 KB) from the official rsms/inter release zip (`web/InterVariable.woff2`) |
| `src/Klasly.Web/wwwroot/fonts/OFL.txt` | added | SIL Open Font License, must ship with the font (`LICENSE.txt` from the same zip) |
| `android/KlaslyApp/app/src/main/res/drawable/ic_launcher_foreground.xml` | modified | White mark in the 66 dp safe zone (also used as the monochrome icon) |
| `android/KlaslyApp/app/src/main/res/drawable/ic_launcher_background.xml` | modified | Solid Klasly Teal instead of the template green grid |
| `docs/brand/klasly-logo-concept.png`, `klasly-app-icon-concept.png` | added | The team's approved concept art (source for the vectors) |
| `tasks/ui-redesign-todo.md` | modified | M0, M1 ticked |

## Key code
```svg
<svg viewBox="0 0 24 24"><g fill="none" stroke="#126B64" stroke-width="4.1" stroke-linecap="round" stroke-linejoin="round">
  <path d="M3.1 2.5v18.9"/>                       <!-- stem -->
  <path d="M3.1 13.25 19.25 3.8"/>                <!-- upper arm -->
  <path d="M7.2 15.1 14.2 21.5 20.75 14.5"/>      <!-- lower leg = check -->
</g></svg>
```
Android uses the same three strokes scaled ×2 and offset by 30 dp (`M36.2,35V72.8 M36.2,56.5L68.5,37.6 M44.4,60.2L58.4,73L71.5,59`, stroke 8.2).

## Database changes
None.

## How it was verified
- Rendered the SVGs next to the concept at 16/32/48/128 px, light and reverse (navy, teal): shape matches, 16 px still reads as K + check.
- Android icon previewed inside circle and rounded-square masks: the mark stays inside the safe zone.
- `head -c 4 InterVariable.woff2` = `wOF2`; licence contains "SIL OPEN FONT LICENSE".
- `dotnet build Klasly.slnx`: 0 warnings, 0 errors. `dotnet test Klasly.slnx`: 163 passed, 0 failed.
- `./gradlew assembleDebug` **not run**: this PC has no JDK (SPEC Decision 10). A teammate must build once.

## Known limits / TODO
- The concept uses rounded caps; SPEC A0 said "square or slightly rounded". The uploaded concept wins.
- The app-icon concept has square-cut strokes; the Android icon uses the rounded mark so web and phone match.
- The standalone logo SVGs use `<text>`, so they show the system font when Inter isn't installed (an `<img>` can't load web fonts). The web navbar uses inline SVG + HTML text (M2), so it always gets Inter.
- `favicon.ico` stays until M2, where `_Layout` links `favicon.svg` (removing it now would only cause 404s).

## Follow-up (2026-11-20)
The JDK at `D:\Android Studio\jbr` exists after all. `./gradlew assembleDebug testDebugUnitTest` with the new launcher drawables: **BUILD SUCCESSFUL** (unit tests pass). The icon was not checked on a device.
