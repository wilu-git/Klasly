# UI redesign — task list

> Plan: [`ui-redesign-plan.md`](./ui-redesign-plan.md) · Spec: [`SPEC.md`](../SPEC.md) (Approved, rev 4) · Branch: `ui-redesign` (parent `dev`, no merge or deploy before the demo on 2026-11-21).
> Each milestone: `dotnet build Klasly.slnx` (0 warnings), `dotnet test Klasly.slnx` (green), browser checks at 320/375/768/1440 px, form-contract diff against `docs/ui-ux/baseline-forms.json`, `/impeccable` on changed views, `graphify update .`, CLAUDE.md §5 report, one commit (no Claude attribution).

- [x] M0 Spec close-out and form baseline (`docs: approve UI redesign spec, record form baseline`)
- [x] M1 ui-brand: K + check mark, **logo gate** (user approves), other SVGs, Inter, Android icon drawables
- [x] M2 ui-foundation: tokens, type, spacing, layout/nav, alerts, `_StatusBadge`, `site.js`, `DESIGN.md`, **review gate**
- [ ] M3 ui-message-pages: StatusCode re-execute, Not found, Error, ApiUnavailable, AccessDenied
- [ ] M4 ui-teacher: error-key split, correction row anchor, class/session/grade pages, **review gate**
- [ ] M5 ui-parent: whole-row links, summary line, at-risk first, labelled numbers
- [ ] M6a ui-admin lists, M6b ui-admin forms
- [ ] M7 ui-auth-polish: login
- [ ] M8 Final QA, /impeccable finish review, /review, SPEC criteria ticked, summary report
