# Implementation Plan: Failed emails page checks (`/impeccable` pass + browser check)

> Built from [`SPEC.md`](../SPEC.md) (open questions 1–3 as recommended). Task details, acceptance criteria and verification are in [`todo-failed-emails-checks.md`](./todo-failed-emails-checks.md).
> `tasks/plan.md` / `tasks/todo.md` are the old Milestone 8 files. They are left untouched, so this plan gets its own files, as M9–M15 did.

## Overview

Close the two open boxes of Milestone 14 Task 8: the `/impeccable` pass on `Admin/FailedEmails/Index.cshtml`, and the browser check (admin sees the list, teacher is refused). This is checking and a narrow polish only. No API, database, Worker or Android change.

## Architecture decisions

- **Check first, change later.** Tasks 1 and 2 are read-only. They produce evidence and a defect list. Only Task 3 edits a file, and only when Task 2 found a defect.
- **The browser check runs first.** It tests existing behaviour, and a teacher getting in would be a security bug. If that happens, stop and report it before any polish (fail fast).
- **The house style is the authority.** There is no PRODUCT.md / DESIGN.md, so the Terms and Accounts pages set the look, as in M13 Task 5. Bootstrap utilities come first, with a `site.css` rule only when no utility class fits.
- **Evidence is screenshots plus DOM measurements.** If the browser pane is hidden and screenshots can't render, use DOM checks (`scrollWidth` vs `clientWidth`, computed colours, element heights) and say so.

## Task list

### Phase 1: Check (read-only)
- [x] Task 1: Browser check: admin list, teacher refused, API-down error
- [x] Task 2: `/impeccable` measurement and defect list

### Checkpoint A (after Tasks 1–2)
- [x] The teacher is refused. If not, stop and report a security finding.
- [x] The defect list is written (D1: narrow-screen column squeeze), so Task 3 runs.

### Phase 2: Fix
- [ ] Task 3: Fix the defects and measure again (only if Task 2 found any)

### Phase 3: Wrap-up
- [ ] Task 4: Build, tests, tick boxes, reports, graph

### Checkpoint: Complete
- [ ] Every success criterion in `SPEC.md` is ticked, with its evidence in the report.

## Risks and mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| A teacher can open the page | High | Stop at Checkpoint A, report it, and fix the controller's `Authorize` in its own task after you agree |
| The browser pane is hidden, so screenshots fail | Medium | Fall back to DOM measurements (the M13 Task 5 method) and note it in the report |
| The API or LocalDB isn't running | Low | Start `api` before `web` from `.claude/launch.json`, and check `preview_logs` |
| The polish grows into a redesign | Low | Spec boundary: fix real defects only, keep the Terms table pattern |

## Open questions

None. Spec questions 1–3 were taken as recommended: empty state checked in markup only, the API-down error checked once, the teacher role only.
