# UI redesign M2 — `ui-foundation`: tokens, layout, navigation, shared JS

## Summary
Milestone M2 of the Web UI/UX redesign (SPEC.md §B–C). `site.css` now holds the Klasly design tokens (teal, navy, canvas, muted text, control borders) mapped onto Bootstrap 5.3 variables, the type scale (16 px body everywhere, 28/32 px page titles), white panels on a grey canvas, and a solid teal focus ring. `_Layout` gets the logo, a skip link, a collapsing role menu with an area-aware active item, and loses the footer, import map and template CSS. Success alerts can be dismissed. A new `_StatusBadge` partial renders every status. `site.js` adds double-submit protection and an unsaved-changes guard. `DESIGN.md` records the design system.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `src/Klasly.Web/wwwroot/css/site.css` | modified | Tokens, Bootstrap overrides, type scale, navbar, buttons, pills, forms, panels/tables, focus; 14 px phone rule and sticky-footer rules removed; QR min width capped at the screen width |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml` | modified | Skip link, `main#main.container`, inline logo, toggler + collapse (Admin `lg`, others `sm`), area-aware active item, `AlertsInPage` switch, `favicon.svg`; footer, import map and scoped-CSS link removed |
| `src/Klasly.Web/Views/Shared/_Layout.cshtml.css` | deleted | Dead template CSS (`#0077cc`, `#1b6ec2`, footer) |
| `src/Klasly.Web/wwwroot/favicon.ico` | deleted | Replaced by `favicon.svg` (M1) |
| `src/Klasly.Web/Views/Shared/_Alerts.cshtml` | modified | Success alert dismissible; error stays |
| `src/Klasly.Web/Views/Shared/_StatusBadge.cshtml` | added | One badge for every status: subtle variants, filled only for "At risk" |
| `src/Klasly.Web/Areas/Teacher/Views/Shared/_SessionStatusBadge.cshtml` | modified | Same status → text logic, renders through `_StatusBadge` ("Scheduled" is info, not primary, so no teal badge) |
| `src/Klasly.Web/wwwroot/js/site.js` | modified | Pending submit + `data-warn-unsaved` guard; `data-confirm` kept |
| `DESIGN.md` | added | Impeccable design file from SPEC §A–B |

## Key code
```cshtml
@* _Layout: every Teacher page highlights "My classes", every Parent page "My children" *@
var activeItem = currentArea != navArea ? null : navArea switch { "Teacher" => "Classes", "Parent" => "Children", _ => currentController };
var breakpoint = navArea == "Admin" ? "lg" : "sm";
```
```js
// site.js: only a confirmed, valid POST gets disabled; data is collected before the button is disabled.
if (event.defaultPrevented) return;            // cancelled confirm or jQuery Validation failure
setTimeout(function () { button.textContent = button.dataset.pendingText || 'Saving…'; button.disabled = true; }, 0);
```
```cshtml
@model (string Text, string Variant)
var css = Model.Variant == "danger-strong" ? "text-bg-danger"
    : $"bg-{Model.Variant}-subtle text-{Model.Variant}-emphasis border border-{Model.Variant}-subtle";
```

## Database changes
None.

## How it was verified
- `dotnet build Klasly.slnx`: 0 warnings, 0 errors. `dotnet test Klasly.slnx`: 163 passed.
- **Form contract:** every page in `docs/ui-ux/baseline-forms.json` (25 page states: login, admin, teacher) fetched as the right role and compared: all identical (action, method, input names incl. `__RequestVerificationToken`); layout Logout form still POST with token.
- **Fonts:** `document.fonts.check('16px Inter')` true; body 16 px; page title 32 px / 600 / navy at 1440 px.
- **Contrast (measured in the page):** body on canvas 11.4:1, muted on canvas 5.1:1, primary button 6.3:1, link 6.3:1, active nav 5.6:1, success badge 10.4:1, input border 3.0:1.
- **Responsive sweep** (iframes at 320/375/768 px across teacher, admin and parent pages): navbar collapses with a working "Menu" toggler (Teacher/Parent below 576, Admin below 992); parent pages, login, access denied, all forms and admin terms/subjects/class details fit. **Still overflowing** (page work, not layout): teacher My classes and class page tables at 320–375, admin Accounts list, Classes list and parent-links form at 320–375. These are fixed in M4 and M6.
- **Pending submit:** empty login → client errors, button stays enabled; valid login → button disabled with "Saving…"; Terms "Deactivate" with the confirm cancelled → button stays enabled.
- Console: no script errors.

## Known limits / TODO
- Badges other than the session badge still use their old classes until their page milestone (M4 teacher roster, M5 parent risk, M6 admin).
- "Saving…" is the default pending text; pages set better text where it matters (Start session, Log in) in M4/M7.
- Before-redesign screenshots were viewed in the browser but not saved as files.
