# How to use the redesigned parent pages

## Prerequisites
API and website running (the Worker must have run at least once for "At risk" / "On track" to show). Log in as parent `marites.mendoza@klasly.test` (password in `docs/TEST-ACCOUNTS.md`).

## Steps
1. **My children:** tap anywhere on a child's row to open their overview.
2. **Child overview:** read the sentence under the name first. It says whether every class is on track or which classes need attention; tap a class code to jump to it.
3. Classes that need attention are listed first. Each card shows Present, Late, Absent and Excused counts, the absence rate and the weighted average.
4. Tap **Attendance records** or **Grades** to see the details. With more than one child, switch with the name buttons under the header.

## Example
"All 3 classes need attention: CPE 401, ELECT3, IT302." in red, then the CPE 401 card with a red "At risk" badge and "0 Present · 0 Late · 2 Absent · 0 Excused".

## Troubleshooting
- **"The early-warning check hasn't run yet this term."**: the Worker hasn't computed risk yet; counts are still live.
- **A parent with one child never sees My children**: by design, they go straight to the overview.
