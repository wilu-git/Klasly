# UI redesign M6 — `ui-admin`: admin lists and forms

## Summary
Milestone M6 (handoff P2-5–P2-8, P2-11, P2-12, P2-13 admin, P3-5–P3-8; SPEC Decision 7). The lists follow one row-action convention (Edit outline, state change outline, destructive red link last, hidden row context for screen readers) and one status badge; secondary columns move under the record name on phones. Admin home lists Failed emails. Terms: every row has a status, and "Make active" asks first and names the term it replaces. Accounts: one "New account" control, one role order, no repeated Role column under a filter. Classes: the class name is the link, and the term filter applies on change. Class details: enroll first, active students first with dropped ones grouped. Failed emails says "the email service" and links to parent accounts. Forms: breadcrumbs, record names in Edit headings, full-width buttons on phones; the account form is grouped into fieldsets and warns about unsaved edits; linked students sit in their own panel marked "saved immediately"; the class form puts Term first and explains a missing (inactive) teacher.

## Projects touched
WEB MVC only.

## Files changed
| Path | Change | Why |
|---|---|---|
| `Areas/Admin/Views/Home/Index.cshtml` | modified | Failed emails added to the section list |
| `Areas/Admin/Views/Terms/Index.cshtml` | modified | Active/Inactive badge on every row, Make active confirm naming the replaced term, dates under the name on phones, row context |
| `Areas/Admin/Views/Subjects/Index.cshtml` | modified | Row context; Units under the title on phones |
| `Areas/Admin/Views/Accounts/Index.cshtml` | modified | One create control (dropdown, or "New {role}" under a filter), role order = `AccountsController.Roles`, Role column hidden under a filter, email/ID/status move under the name on smaller screens, outline Reset phone, row context |
| `Areas/Admin/Views/Classes/Index.cshtml` | modified | Class name links to Details, Edit only as row action, filter submits on change (`<noscript>` Show), all-terms empty state |
| `Areas/Admin/Views/Classes/Details.cshtml` | modified | Enroll panel above roster (`btn-outline-primary`), active first, "Dropped (N)" group, `_StatusBadge` |
| `Areas/Admin/Views/FailedEmails/Index.cshtml` | modified | "the email service" (L10, L16), link to `Accounts?role=Parent` |
| `Areas/Admin/Views/Terms/Form.cshtml`, `Subjects/Form.cshtml` | modified | Breadcrumb, record name in Edit heading, `d-grid d-sm-flex` buttons |
| `Areas/Admin/Views/Classes/Form.cshtml` | modified | Breadcrumb (Classes › class › Edit), Term first, inactive-teacher help text |
| `Areas/Admin/Views/Accounts/Form.cshtml` | modified | Breadcrumb, fieldsets Login / {Role} details / Status, status help text, `data-warn-unsaved` |
| `Areas/Admin/Views/Accounts/_ParentLinks.cshtml` | modified | `.k-panel`, "Changes here are saved immediately.", columns under the name on phones, row context, "Linking…" |

## Key code
```cshtml
@* Terms: switching the active term names what it replaces *@
data-confirm="@(activeTerm is null ? $"Make {term.Name} the active term?" : $"Make {term.Name} the active term? {activeTerm.Name} will be deactivated.")"

@* Class form: saved teacher no longer in the active list *@
var teacherMissing = Model.TeacherId is { } teacherId && !Model.TeacherOptions.Any(o => o.Value == teacherId.ToString());
```

## Database changes
None.

## How it was verified
- `dotnet build src/Klasly.Web`: 0 warnings, 0 errors.
- **Form contract:** all 18 admin baseline pages compared. Identical except Class details, where the same two forms now come in a different order (Enroll first; approved in the plan).
- **Responsive** (iframes at 320/375/768/1440 px over 14 admin pages): no page scrolls sideways; no table box scrolls except Failed emails, which SPEC B6 allows.
- Parent account edit at desktop: breadcrumb "Accounts › Parents › Linda Bautista", heading "Edit parent: Linda Bautista", fieldsets, linked-students panel.

## Known limits / TODO
- One commit instead of the planned two (lists + forms), since both were verified together.
- The account list still has no search (optional feature, not asked for).
